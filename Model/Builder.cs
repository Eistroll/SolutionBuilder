using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SolutionBuilder
{

    class Builder
    {
        public string BaseDir;
        public string BaseOptions;
        public FileInfo BuildExe;
        public ObservableCollection<SolutionObjectView> solutions;
        public Action<string> AddToLog;
        public Action<int, int, int, string, bool> UpdateProgress;
        public bool Build(CancellationToken token)
        {
            bool atLeastOneBuildFailed = false;
            if (solutions.Count == 0)
                return false;
            //(FindResource("showMe") as Storyboard).Begin();
            UpdateProgress?.Invoke(0, solutions.Count, 0, "", atLeastOneBuildFailed);
            int count = 0;
            foreach (SolutionObjectView solution in solutions)
            {
                if (token.IsCancellationRequested == true)
                {
                    AddToLog?.Invoke("Build has been canceled.");
                    UpdateProgress?.Invoke(0, solutions.Count, solutions.Count, "Build has been canceled.", atLeastOneBuildFailed);
                    //token.ThrowIfCancellationRequested();
                    return false;
                }
                string solutionPath = BaseDir + "\\" + solution.Name;
                UpdateProgress?.Invoke(0, solutions.Count, count, solutionPath, atLeastOneBuildFailed);
                bool failure = BuildSolution(BuildExe, solutionPath, BaseOptions, solution);
                atLeastOneBuildFailed = atLeastOneBuildFailed || failure;
                AddToLog?.Invoke(solutionPath + (failure ? " failed" : " successful") + Environment.NewLine);
                UpdateProgress?.Invoke(0, solutions.Count, ++count, solutionPath, atLeastOneBuildFailed);
            }
            return atLeastOneBuildFailed;
        }
        private void BuildOutputHandler(object sender, DataReceivedEventArgs e, SolutionObjectView solution)
        {
            string line = e.Data + Environment.NewLine;
            solution.BuildLog += line;
        }
        private bool BuildSolution(FileInfo buildExe, string solutionPath, string baseOptions, SolutionObjectView solution)
        {
            bool buildFailure = true;
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo()
            { WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            startInfo.FileName = buildExe.ToString();
            startInfo.Arguments = baseOptions + " " + solution.Options + " " + solutionPath;
            process.StartInfo = startInfo;
            // Set event handler
            process.OutputDataReceived += (s, eventargs) => BuildOutputHandler(s, eventargs, solution);
            bool Success = process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            int exitCode = process.ExitCode;
            if (exitCode == 0)
            {
                buildFailure = false;
                solution.BuildState = View.State.Success;
            }
            else
            {
                buildFailure = true;
                solution.BuildState = View.State.Failure;
            }

            solution.SuccessFlag = exitCode == 0 ? true : false;
            return buildFailure;
        }
    }
}
