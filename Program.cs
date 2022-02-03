using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DDXTextureCompressor
{
    internal class Program
    {
        // TexConv Default Parameters
        private static readonly ProcessStartInfo TexConvInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "texconv.exe",
            WindowStyle = ProcessWindowStyle.Hidden
        };
        private static readonly Process TexConvProc = new Process { StartInfo = TexConvInfo, EnableRaisingEvents = true };
        private static int TexConvAsyncActionLimit = 10;

        // Constants
        private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB" };
        private static readonly DirectoryInfo buildPath = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "DDXTC_BuildPath"));

        // Containers
        private static readonly List<Action> TexConvActions = new List<Action>();
        private static DirectoryInfo ParentDirectory;
        private static bool InputIsDirectory;


        private static void Main(string[] args)
        {
            #region Initialization
            Console.WriteLine("-------------------------------------------------------------------");
            Console.WriteLine("DDX Texture Compressor\n\nA tool for compressing .ddx files for Halo Wars: Defintive Edition.\n");
            Console.WriteLine("Software made by: Half Dragon#3008");
            Console.WriteLine("Github: https://github.com/HalfDragonLucy/DDXCompressor");
            Console.WriteLine("-------------------------------------------------------------------\n");

            // Check if any input was provided
            if (args.Length <= 0)
            {
                DisplayHelpText();
                return;
            }

            // Check if an action limit was specified
            if (args.Length > 1)
                int.TryParse(args[1], out TexConvAsyncActionLimit);
            Console.WriteLine($"> Maximum parallel tasks: {TexConvAsyncActionLimit}");

            // Set DirectoryCheck
            InputIsDirectory = (File.GetAttributes(args[0]) & FileAttributes.Directory) == FileAttributes.Directory;
            Console.WriteLine($"> Given path points to a {(InputIsDirectory ? "directory" : "file")}\n");

            // Create build directory
            if (!buildPath.Exists)
                Directory.CreateDirectory(buildPath.FullName);
            #endregion

            #region Single File Check
            // Check if given path is a file
            if (!InputIsDirectory)
            {
                CreateDDS(new FileInfo(args[0]));
                return;
            }
            #endregion

            #region Full Directory Scan
            // Given path is a directory
            ParentDirectory = new DirectoryInfo(args[0]);

            // Create all subdirs in buildPath and get all FullNames of files
            ScrapeParentDirectory(ParentDirectory);

            // Run all DDX conversions
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = TexConvAsyncActionLimit};
            Parallel.Invoke(options, TexConvActions.ToArray());
            #endregion

            Console.WriteLine("\nFinished!\n\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void ScrapeParentDirectory(DirectoryInfo currentDir)
        {
            // Create matching subdirectory in buildPath
            if (!Directory.Exists(GetBuildPath(currentDir.FullName)))
                Directory.CreateDirectory(GetBuildPath(currentDir.FullName));

            // Get all filenames in current directory
            // If file is not a .ddx file, go ahead and move it
            foreach (FileInfo file in currentDir.EnumerateFiles())
            {
                if (file.Extension != ".ddx" || currentDir.Name == "ui")
                    file.CopyTo(GetBuildPath(file.FullName), overwrite: true);
                else
                    TexConvActions.Add(() => { CreateDDS(file); });
            }

            // For each subdir in a given directory, get all files in there too
            foreach (DirectoryInfo subDir in currentDir.GetDirectories())
                ScrapeParentDirectory(subDir);   
        }

        private static void CreateDDS(FileInfo currentDDX)
        {
            // Secondary extension check
            if (currentDDX.Extension != ".ddx")
            {
                Console.WriteLine("Given input file is not a .ddx (renamed .dds) file!");
                return;
            }

            // Convert!
            try
            {
                // Rename given .ddx file to .dds
                FileInfo tempDDS = new FileInfo(Path.ChangeExtension(currentDDX.FullName, ".dds"));
                File.Move(currentDDX.FullName, tempDDS.FullName);

                // Ensure that output can be overwritten

                // Configure extra start info and launch the converter
                TexConvProc.StartInfo.Arguments = $"\"{tempDDS.FullName}\" -r:keep -pow2 -ft dds -y -o \"{GetBuildPath(tempDDS.DirectoryName)}\" -gpu 0 -f DXT1 -dx9";
                TexConvProc.Start();

                // Wait until process finishes
                TexConvProc.WaitForExit();

                // Rename original file
                File.Move(tempDDS.FullName, currentDDX.FullName);
                
                // Delete any existing newly generated .ddx file
                if (File.Exists(GetBuildPath(currentDDX.FullName)))
                    File.Delete(GetBuildPath(currentDDX.FullName));

                // Rename newly generated file back to .ddx; the new FileInfo object is to get the updated size info
                File.Move(GetBuildPath(tempDDS.FullName), GetBuildPath(currentDDX.FullName));
                Console.WriteLine($"File {currentDDX.Name} optimization status: {GetSizeSuffix(currentDDX.Length)} -> {GetSizeSuffix(new FileInfo(GetBuildPath(currentDDX.FullName)).Length)}");
            }
            catch (Exception ex)
            {
                ExceptionDump(ex);
            }
        }

        private static void DisplayHelpText()
        {
            Console.WriteLine("Usage (Brackets Indicate Optional Parameters):");
            Console.WriteLine("  Command Line:\n\tDDXTextureCompressor.exe \"Full Path to File or Directory\" [Max # of Tasks]");
            Console.WriteLine("\n  Or simply drag and drop your file/folder onto DDXTextureCompressor.exe");
            Console.ReadKey();
        }

        private static void ExceptionDump(Exception exception)
        {
            // Prints in detail what error has occurred
            Console.WriteLine($"Error Source: {exception.Source}");
            Console.WriteLine($"Error Message: {exception.Message}");
            Console.WriteLine($"StackTrace:\n{exception.StackTrace}\n");
            Console.WriteLine($"Inner Exceptions:\n{exception.InnerException}\n");
        }

        #region String Formatting Functions
        private static string GetSizeSuffix(long value)
        {
            int suffixIndex;
            double dValue = value;

            // Check if given value is a negative number
            if (value < 0) { return $"-{GetSizeSuffix(-value)}"; }

            // Divide value by 1024 until value can no longer be divided evenly
            for (suffixIndex = 0; suffixIndex < SizeSuffixes.Length && value >= 1024; suffixIndex++, value /= 1024)
                dValue = value / 1024.0;

            // Format output to use the correct suffix
            return string.Format("{0:n1} {1}", dValue, SizeSuffixes[suffixIndex]);
        }

        private static string GetBuildPath(string path)
        {
            return path.Replace(ParentDirectory.FullName, buildPath.FullName);
        }
        #endregion
    }
}
