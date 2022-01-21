using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace DDXTextureCompressor
{
    internal class Program
    {
        private static readonly List<Task> tasks = new List<Task>();
        private static readonly string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        private static void Main(string[] args)
        {
            Console.WriteLine("Please do not click this window while it's being used. \n");

            if (args.Length > 0 && Directory.Exists(args[0]))
            {
                for (int i = 0; i < args.Length; i++)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(args[i]);
                    DirectoryInfo dest = new DirectoryInfo(directoryInfo.FullName + "_Build");
                    CopyDirectoryTree(directoryInfo, dest);
                    ParseDirectories(dest.FullName);
                }
            }
            if (args.Length > 0 && File.Exists(args[0]))
            {
                FileInfo file = new FileInfo(args[0]);
                CreateDDS(file);
            }

            Console.WriteLine("\nFinished!");
            Console.WriteLine("Software made by: Half Dragon#3008");
            Console.WriteLine("Github: https://github.com/HalfDragonLucy/DDXCompressor");
            Console.ReadKey();
        }

        private static void DisplayEndInfo(FileInfo oldfile, long newfile)
        {
            long oldSize = oldfile.Length;
            long newSize = newfile;
            Console.WriteLine($"File optimization status: {oldfile} {SizeSuffix(oldSize)} > {SizeSuffix(newSize)}");
        }

        private static void CreateDDS(FileInfo file)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

                File.Move(file.FullName, Path.ChangeExtension(file.FullName, ".dds"));
                string f = Path.ChangeExtension(file.FullName, ".dds");

                Process proc = new Process();
                proc.StartInfo.FileName = "texconv.exe";
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.EnableRaisingEvents = true;

                proc.StartInfo.Arguments = $"{f} -r -pow2 -ft dds -y -o {file.DirectoryName} -nologo -f DXT1";
                proc.Start();
                while (!proc.HasExited)
                {
                }

                File.Move(f, Path.ChangeExtension(f, ".ddx"));
                long newFileSize = f.Length;
                DisplayEndInfo(file, newFileSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        private static void ProcessDirectory(DirectoryInfo directory)
        {
            foreach (FileInfo file in directory.EnumerateFiles("*.ddx"))
            {
                tasks.Add(Task.Run(() => { CreateDDS(file); }));
            }
            Task t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch { }
        }

        private static void ParseDirectories(string root)
        {
            ProcessDirectory(new DirectoryInfo(root));

            string[] subDirectories = Directory.GetDirectories(root);

            if (subDirectories.Length == 0)
            {
                return;
            }

            foreach (string subDirectory in subDirectories)
            {
                ParseDirectories(subDirectory);
            }
        }

        private static void CopyDirectoryTree(DirectoryInfo source, DirectoryInfo dest)
        {
            if (!Directory.Exists(dest.FullName))
            {
                Directory.CreateDirectory(dest.FullName);
            }

            foreach (FileInfo file in source.EnumerateFiles())
            {
                file.CopyTo(Path.Combine(dest.ToString(), file.Name), overwrite: true);
            }

            foreach (DirectoryInfo subDirectory in source.GetDirectories())
            {
                DirectoryInfo newDirectory = dest.CreateSubdirectory(subDirectory.Name);
                CopyDirectoryTree(subDirectory, newDirectory);
            }
        }

        private static string SizeSuffix(long value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }

            int i = 0;
            decimal dValue = value;
            while (Math.Round(dValue / 1024) >= 1)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n1} {1}", dValue, SizeSuffixes[i]);
        }
    }
}
