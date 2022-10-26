
using System.Diagnostics;
using taskium.server;

namespace microhobby.Utils
{
    class TaskExecuter
    {
        public TaskExecuter(taskium.server.Task taskDefinition)
        {
            var taskiumRoot = Environment
                .GetEnvironmentVariable("TASKIUM_STORAGE_ROOT");
            if (string.IsNullOrEmpty(taskiumRoot)) 
                throw new Exception("TASKIUM_STORAGE_ROOT not set");

            new Thread(() => {
                StreamWriter? stdOutputSW = null;
                StreamWriter? stdErrSW = null;
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo("/usr/bin/pwsh");

                var args = new string[]{
                    "-nop",
                    "-f",
                    $"{taskiumRoot}/.taskium/taskExecuter.ps1",
                    $"{taskDefinition.Id}",
                    $"{taskDefinition.Repo}",
                    $"{taskDefinition.Branch}",
                    $"{taskDefinition.TaskLabel}"
                };

                Array.ForEach(
                    args,
                    arg => process.StartInfo.ArgumentList.Add(arg)
                );

                process.StartInfo.WorkingDirectory = $"{taskiumRoot}/.taskium";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.OutputDataReceived += (sender, data) => {
                    var db = TaskDb.GetDBFromNewScope();
                    var str = "";
                    if (data.Data != null)
                        str = data.Data.Contains("\n") 
                                ? data.Data
                                : $"{data.Data}\n";
                    
                    taskDefinition.StdOut += str;
                    
                    try {
                        if (stdOutputSW == null) {
                            if (Directory.Exists($"{taskiumRoot}/.taskium/{taskDefinition.Id}/")) {
                                stdOutputSW = File.AppendText(
                                    $"{taskiumRoot}/.taskium/{taskDefinition.Id}/out.log"
                                );

                                stdOutputSW.Write(str);
                            }
                        }

                        db.Update(taskDefinition);
                        db.SaveChanges();
                    } catch {
                        // TODO: inject logger from api scope
                    }
                };

                process.ErrorDataReceived += (sender, data) => {
                    var db = TaskDb.GetDBFromNewScope();
                    var str = "";
                    if (data.Data != null)
                        str = data.Data.Contains("\n") 
                                ? data.Data
                                : $"{data.Data}\n";

                    taskDefinition.StdOut += str;

                    try {
                        if (stdOutputSW == null) {
                            if (Directory.Exists($"{taskiumRoot}/.taskium/{taskDefinition.Id}/")) {
                                stdOutputSW = File.AppendText(
                                    $"{taskiumRoot}/.taskium/{taskDefinition.Id}/out.log"
                                );

                                stdOutputSW.Write(str);
                            }
                        }

                        db.Update(taskDefinition);
                        db.SaveChanges();
                    } catch {
                        // TODO: inject logger from api scope
                    }
                };

                process.Exited += (sender, data) => {
                    var db = TaskDb.GetDBFromNewScope();
                    stdErrSW?.Flush();
                    stdErrSW?.Close();
                    stdOutputSW?.Flush();
                    stdOutputSW?.Close();

                    taskDefinition.ReturnCode = process.ExitCode;
                    taskDefinition.IsComplete = true;

                    db.Update(taskDefinition);
                    db.SaveChanges();
                };

                // start it
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExitAsync();
            }).Start();
        }
    }
}
