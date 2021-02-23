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

namespace PluginDb2
{
    class Program
    {
        // private const string LDLibrary = "LD_LIBRARY_PATH";

        static void Main(string[] args)
        {
            try
            {
                // configure env
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var installDirectory = Path.GetDirectoryName(assemblyPath);

                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")))
                {
                    Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", $"{installDirectory}/clidriver/lib");

                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = $"{Path.GetFileNameWithoutExtension(assemblyPath)}{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "")}",
                            Arguments = "",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    proc.Start();

                    while (!proc.StandardOutput.EndOfStream)
                    {
                        var line = proc.StandardOutput.ReadLine();
                        Console.WriteLine(line);
                    }
                }

                // setup logger
                Logger.Init();

                // Add final chance exception handler
                AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                {
                    Logger.Error(null, $"died: {eventArgs.ExceptionObject}");
                    Logger.CloseAndFlush();
                };

                // create new server and start it
                Server server = new Server
                {
                    Services = {Publisher.BindService(new Plugin.Plugin())},
                    Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
                };
                server.Start();

                // write out the connection information for the Hashicorp plugin runner
                var output = String.Format("{0}|{1}|{2}|{3}:{4}|{5}",
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
                Logger.Error(e, e.Message);
                Logger.CloseAndFlush();
                throw;
            }
        }
    }
}