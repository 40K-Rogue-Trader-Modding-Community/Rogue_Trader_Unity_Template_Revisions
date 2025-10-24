using System;
using System.Diagnostics;
using System.Text;
using Debug = UnityEngine.Debug;

#nullable enable

namespace Code.Framework.Editor.VersionControl
{
    public class OsCommand
    {
        private readonly Encoding outputEncoding;
        private readonly bool logErrors;

        private BinaryStreamAccumulator? outAcc;
        public string? StdOut => outAcc?.GetString(outputEncoding);

        private BinaryStreamAccumulator? errAcc;
        public string? StdErr => errAcc?.GetString(outputEncoding);

        public OsCommand(Encoding outputEncoding, bool logErrors = false)
        {
            this.outputEncoding = outputEncoding;
            this.logErrors = logErrors;
        }

        public bool Execute(string executable, string args, string workingDir="")
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    WorkingDirectory = workingDir,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = outputEncoding,
                    RedirectStandardError = true,
                    StandardErrorEncoding = outputEncoding,
                },
            };

            try
            {
                process.Start();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                Debug.LogError("Failed to start external process.");
                return false;
            }

            outAcc = new BinaryStreamAccumulator(process.StandardOutput.BaseStream);
            errAcc = new BinaryStreamAccumulator(process.StandardError.BaseStream);

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                return true;
            }

            if (logErrors)
            {
                string errorOutput = errAcc.GetString(outputEncoding);
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    Debug.LogError(errorOutput);
                }

                string stdOutput = outAcc.GetString(outputEncoding);
                if (!string.IsNullOrEmpty(stdOutput))
                {
                    Debug.LogError(stdOutput);
                }

                Debug.LogError("OS command failed.");
            }

            return false;
        }
    }
}