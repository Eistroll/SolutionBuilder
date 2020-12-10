using SolutionBuilder.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SolutionBuilder
{
    class Distributor
    {
        public string copyExe;
        public string source;
        public string target;
        string options = "/MIR";
        public Action<string> AddToLog;

        public void Copy(CancellationToken token)
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo()
                { WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardOutput = false, UseShellExecute = false, CreateNoWindow = true };
                startInfo.FileName = @copyExe;
                startInfo.Arguments = $"\"{source}\" \"{target}\" \"{options}\"";
                process.StartInfo = startInfo;
                //process.OutputDataReceived += (s, eventargs) => BuildOutputHandler(s, eventargs, solution);
                bool started = process.Start();
                //process.BeginOutputReadLine();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                switch (exitCode)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3: AddToLog?.Invoke($"Finished with code {exitCode} (Success): Copy\n{source} -> {target}" + Environment.NewLine); break;
                    case 4:
                    case 5:
                    case 6:
                    case 7: AddToLog?.Invoke($"Finished with code {exitCode} (Warning): Copy\n{source} -> {target}" + Environment.NewLine); break;
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15: AddToLog?.Invoke($"Finished with code {exitCode} (Error): Copy\n{source} ->  to {target}" + Environment.NewLine); break;
                    case 16: AddToLog?.Invoke($"Finished with code {exitCode}: did not run" + Environment.NewLine); break;
                }
            }
            catch (System.Exception ex)
            {
                AddToLog(ex.Message + Environment.NewLine);
            }
        }
    }
}
