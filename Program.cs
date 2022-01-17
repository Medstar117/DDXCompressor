using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace HWTextureCompressor
{
    internal class Program
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private static void Main(string[] args)
        {
            IntPtr handle = GetConsoleWindow();

            ShowWindow(handle, 5);

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
        }

        private static void CreateDDS(FileInfo file)
        {
            File.Move(file.FullName, file.FullName.Replace(Path.GetExtension(file.FullName), ".dds"));
            string path = file.FullName.Replace(Path.GetExtension(file.FullName), ".dds");

            Process proc = new Process();
            proc.StartInfo.FileName = $"{Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}/texconv.exe";
            proc.StartInfo.UseShellExecute = true;
            proc.EnableRaisingEvents = true;

            proc.StartInfo.Arguments = $"{path} -w 256 -h 256 -ft dds -y -o {file.DirectoryName} -f DXT2";

            proc.Start();
            proc.WaitForExit();

            File.Move(path, path.Replace(Path.GetExtension(path), ".ddx"));
        }

        private static void ProcessDirectory(DirectoryInfo directory)
        {
            foreach (FileInfo file in directory.EnumerateFiles("*.ddx"))
            {
                CreateDDS(file);
            }
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
    }
}
