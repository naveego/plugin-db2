using System;

namespace PluginDb2
{
    public class Setup
    {
        private static bool _environmentSet = false;
        
        public static void EnsureEnvironment()
        {
            if (_environmentSet) return;

            var homePath = Environment.ExpandEnvironmentVariables("%HOME%");
            var packagePath = $"{homePath}/.nuget/packages/ibm.data.db2.core-osx/1.3.0.100";
            var driverPath = $"{packagePath}/build/clidriver";
            var libPath = $"{driverPath}/lib";
            var binPath = $"{driverPath}/bin";
            
            // 
            //Environment.SetEnvironmentVariable("DB2_CLI_DRIVER_INSTALL_PATH", driverPath);
            //Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", $"{libPath}:{libPath}/icc");

            var currentPath = Environment.GetEnvironmentVariable("PATH");
            //Environment.SetEnvironmentVariable("PATH", $"{binPath}:{currentPath}");
        }
    }
}