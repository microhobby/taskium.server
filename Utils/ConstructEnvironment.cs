
using System.Diagnostics;
using taskium.server;

namespace microhobby.Utils 
{
    static class ConstructEnvironment
    {
        public static void Constructium()
        {
            var taskiumRoot = Environment
                .GetEnvironmentVariable("TASKIUM_STORAGE_ROOT");
            if (string.IsNullOrEmpty(taskiumRoot)) 
                throw new Exception("TASKIUM_STORAGE_ROOT not set");

            if (!Directory.Exists($"{taskiumRoot}/.taskium")) {
                Directory.CreateDirectory($"{taskiumRoot}/.taskium");

                File.Copy(
                    "./Assets/taskExecuter.ps1",
                    $"{taskiumRoot}/.taskium/taskExecuter.ps1"
                );
            }
        }
    }
}
