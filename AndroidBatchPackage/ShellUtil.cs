using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidBatchPackage {
    public class ShellUtil {
        private static ILog log = LogManager.GetLogger(typeof(ShellUtil));

        public static string Execute(string command, string args, string workingDir = null, int timeLimitSeconds = -1) {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo {
                FileName = command,
                Arguments = Encoding.Default.GetString(Encoding.UTF8.GetBytes(args)),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            process.StartInfo.EnvironmentVariables["JAVA_HOME"] = @"C:\Program Files\Android\Android Studio\jre";

            if (workingDir != null) {
                process.StartInfo.WorkingDirectory = workingDir;
            }

            StringBuilder resultBuffer = new StringBuilder();
            StringBuilder errorBuffer = new StringBuilder();
            process.OutputDataReceived += (s, e) => {
                log.Info(e.Data);
                resultBuffer.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (s, e) => {
                log.Info(e.Data);
                errorBuffer.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            if (timeLimitSeconds == -1) {
                process.WaitForExit();
            } else {
                bool graceful = process.WaitForExit(timeLimitSeconds * 1000);
                if (!graceful) {
                    process.Kill();
                }
                return $"Execute time out: {timeLimitSeconds}s, result:\n" + resultBuffer.ToString();
            }
            if (process.ExitCode != 0) {
                log.Error(errorBuffer.ToString());
                throw new InvalidDataException($"exe {command} fail");
            }
            return resultBuffer.ToString();
        }

    }
}
