using System;
using System.IO;
using System.Threading.Tasks;

namespace WordSearchConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Введіть заборонені слова через кому: ");
            string[] bannedWords = Console.ReadLine().Split(',');

            Console.Write("Введіть папку збереження: ");
            string outputFolder = Console.ReadLine();

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            Console.WriteLine("Пошук запущено...");

            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                if (!drive.IsReady) continue;
                SearchDirectory(drive.RootDirectory.FullName, bannedWords, outputFolder);
            }

            Console.WriteLine("Готово!");
            Console.ReadKey();
        }

        static void SearchDirectory(string folder, string[] bannedWords, string outputFolder)
        {
            string[] files;
            try { files = Directory.GetFiles(folder); }
            catch { return; }

            Parallel.ForEach(files, file =>
            {
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
                        Console.WriteLine("Знайдено: " + file);
                    }
                }
                catch { }
            });

            string[] subDirs;
            try { subDirs = Directory.GetDirectories(folder); }
            catch { return; }

            foreach (string dir in subDirs)
                SearchDirectory(dir, bannedWords, outputFolder);
        }
    }
}