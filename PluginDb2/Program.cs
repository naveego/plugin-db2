using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Plugins;
using PluginDb2.Helper;
using Serilog;

namespace PluginDb2
{
    class Program
    {
        // private const string LDLibrary = "LD_LIBRARY_PATH";

        static void Main(string[] args)
        {
            try
            {
                // setup logger
                Logger.Init();
                
                Logger.Info("Starting Db2 Plugin");
                
                // configure env
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var installDirectory = Path.GetDirectoryName(assemblyPath);

                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LD_LICENSE_PATH")))
                {
                    var licenseSourceDirectory = Environment.GetEnvironmentVariable("LD_LICENSE_PATH");
                    var licenseTargetDirectory = Path.Join(installDirectory, "/clidriver/license");
                    Logger.Info($"Loading Driver License from '{licenseSourceDirectory}' to '{licenseTargetDirectory}'.");
                    DirectoryCopy(licenseSourceDirectory, licenseTargetDirectory, true);
                }

                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Logger.Info("LD_LIBRARY_PATH was not set, spawning process as child.");
                    Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", $"{installDirectory}/clidriver/lib");

                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = $"{Path.GetFileNameWithoutExtension(assemblyPath)}",
                            Arguments = "",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                        }
                    };
                    proc.EnableRaisingEvents = true;
                    proc.OutputDataReceived += (s, a) => Console.Out.WriteLine(a.Data);
                    proc.ErrorDataReceived += (s, a) => Console.Error.WriteLine(a.Data);
         
                    proc.Start();

                    proc.BeginErrorReadLine();
                    proc.BeginOutputReadLine();
                    
                    Task.Run(() =>
                    {
                        // wait to exit until given input
                        Console.ReadLine();
                        Logger.Info("Got shutdown signal, plugin stopping child and exiting...");
                        Logger.CloseAndFlush();
                        proc.Kill(true);
                    });
                    
                    proc.WaitForExit();
                    
                    return;
                }

                // Add final chance exception handler
                AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                {
                    Logger.Error(null, $"died: {eventArgs.ExceptionObject}");
                    Logger.CloseAndFlush();
                };
                Logger.Info("Starting Plugin Server");
                
                var ldPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
                Logger.Info($"Using LD_LIBRARY_PATH={ldPath}");
                
                // create new server and start it
                Server server = new Server
                {
                    Services = {Publisher.BindService(new Plugin.Plugin())},
                    Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
                };
                server.Start();

                // write out the connection information for the Hashicorp plugin runner
                var output = string.Format("{0}|{1}|{2}|{3}:{4}|{5}",
                    1, 1, "tcp", "localhost", server.Ports.First().BoundPort, "grpc");

                Console.WriteLine(output);

                Logger.Info("Started on port " + server.Ports.First().BoundPort);

                // wait to exit until given input
                Console.ReadLine();

                Logger.Info("Plugin exiting...");
                Logger.CloseAndFlush();

                // shutdown server
                server.ShutdownAsync().Wait();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Fatal error:\n" + e);
                Logger.Error(e, e.Message);
                Logger.CloseAndFlush();
                Environment.Exit(1);
            }
        }
        
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
        
            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);        

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}