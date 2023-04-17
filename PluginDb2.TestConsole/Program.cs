using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Aunalytics.Sdk.Testing;
using PluginDb2.Helper;

namespace PluginDb2.TestConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Logger.Init();
            Logger.Info("Starting Tests");
            var scenario = new PluginTestScenarioBuilder<Plugin.Plugin>()
                .Configure(config =>
                {
                    config.LogDirectory = "logs";
                    config.PermanentDirectory = "perm";
                    config.TemporaryDirectory = "temp";
                })
                .Read(read =>
                {

                });

            await scenario.RunAsync();
        }
    }
}