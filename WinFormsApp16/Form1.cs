using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp16
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource cts;
        private ManualResetEvent pauseEvent = new ManualResetEvent(true);
        private static Mutex mutex = new Mutex(true, "WordSearchApp");
        private int processedFiles = 0;

        public Form1()
        {
            InitializeComponent();
            if (!mutex.WaitOne(0))
            {
                MessageBox.Show("Програма вже запущена!");
                Application.Exit();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            cts = new CancellationTokenSource();
            pauseEvent.Set();
            processedFiles = 0;
            listBox1.Items.Clear();
            progressBar1.Value = 0;
            progressBar1.Maximum = 100;

            string[] bannedWords = textBox1.Text.Split(',');
            string outputFolder = textBox2.Text;

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            Task.Run(() =>
            {
                DriveInfo[] drives = DriveInfo.GetDrives();

                foreach (DriveInfo drive in drives)
                {
                    if (!drive.IsReady) continue;
                    SearchDirectory(drive.RootDirectory.FullName, bannedWords, outputFolder, cts.Token);
                }

                Invoke(new Action(() =>
                {
                    progressBar1.Value = 100;
                    label1.Text = "Готово! Знайдено: " + listBox1.Items.Count;
                }));
            });
        }

        private void SearchDirectory(string folder, string[] bannedWords, string outputFolder, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            string[] files;
            try { files = Directory.GetFiles(folder); }
            catch { return; }

            Parallel.ForEach(files, new ParallelOptions { CancellationToken = token }, file =>
            {
                pauseEvent.WaitOne();
                if (token.IsCancellationRequested) return;

                try
                {
                    string text = File.ReadAllText(file);
                    bool found = false;

                    foreach (string word in bannedWords)
                    {
                        if (text.Contains(word))
                        {
                            found = true;
                            text = text.Replace(word, "*******");
                        }
                    }

                    if (found)
                    {
                        File.Copy(file, Path.Combine(outputFolder, Path.GetFileName(file)), true);
                        File.WriteAllText(Path.Combine(outputFolder, Path.GetFileName(file) + ".cleaned.txt"), text);

                        Invoke(new Action(() =>
                        {
                            listBox1.Items.Add(file);
                            int val = listBox1.Items.Count % 100;
                            progressBar1.Value = val == 0 ? 99 : val;
                            label1.Text = "Знайдено: " + listBox1.Items.Count;
                        }));
                    }
                }
                catch { }
            });

            string[] subDirs;
            try { subDirs = Directory.GetDirectories(folder); }
            catch { return; }

            foreach (string dir in subDirs)
                SearchDirectory(dir, bannedWords, outputFolder, token);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            pauseEvent.Reset();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            pauseEvent.Set();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            cts.Cancel();
        }
    }
}