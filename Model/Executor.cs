using SolutionBuilder.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionBuilder
{
    public class Executor
    {
        private MainViewModel _ViewModel;
        public Executor(MainViewModel viewModel)
        {
            _ViewModel = viewModel;
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

        public bool BuildSolutions(BuildTabItem tab, FileInfo buildExe, ObservableCollection<SolutionObjectView> solutions = null, Action<string> AddToLog = null, Action<int,int,int> UpdateProgress = null)
        {
            bool atLeastOneBuildFailed = false;
            bool ignoreSelection = true;
            if (solutions == null)
            {
                solutions = tab.Solutions;
                ignoreSelection = false;
            }
            Collection<SolutionObjectView> solutionsToBuild = new Collection<SolutionObjectView>();
            foreach (SolutionObjectView solution in solutions)
            {
                if (ignoreSelection || solution.Checked)
                {
                    solutionsToBuild.Add(solution);
                }
            }
            if (solutionsToBuild.Count == 0)
                return true;
            tab.ProgressVisible = true;
            UpdateProgress?.Invoke(0, solutionsToBuild.Count, 0);
            int count = 0;
            string baseDir = _ViewModel.GetSetting("BaseDir", tab.Header);
            foreach (SolutionObjectView solution in solutionsToBuild)
            {
                string solutionPath = baseDir + "\\" + solution.Name;
                bool failure = BuildSolution(buildExe, solutionPath, tab.BaseOptions, solution);
                atLeastOneBuildFailed = atLeastOneBuildFailed || failure;
                AddToLog?.Invoke(solutionPath + (failure ? " failed" : " successful") + Environment.NewLine);
                UpdateProgress?.Invoke(0, solutionsToBuild.Count, ++count);

            }
            tab.ProgressVisible = false;
            return atLeastOneBuildFailed;
        }
        public void Copy(string copyExe, string source, string target, DistributionItem distribution, Action<string> AddToLog = null)
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo()
                { WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardOutput = false, UseShellExecute = false, CreateNoWindow = true };
                startInfo.FileName = @copyExe;
                string Platform = distribution.Platform;
                target = target.Replace(@"{Platform}", Platform);
                target = target.Replace(@"{Name}", distribution.Folder);
                source = source.Replace(@"{Platform}", Platform);
                string options = "/MIR";
                startInfo.Arguments = $"{source} {target} {options}";
                process.StartInfo = startInfo;
                //process.OutputDataReceived += (s, eventargs) => BuildOutputHandler(s, eventargs, solution);
                AddToLog?.Invoke($"Start: Copy {source} to {target}" + Environment.NewLine);
                bool started = process.Start();
                //process.BeginOutputReadLine();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                switch (exitCode)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3: AddToLog?.Invoke($"Finished with code {exitCode} (Success): Copy {source} to {target}" + Environment.NewLine); break;
                    case 4:
                    case 5:
                    case 6:
                    case 7: AddToLog?.Invoke($"Finished with code {exitCode} (Warning): Copy {source} to {target}" + Environment.NewLine); break;
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15: AddToLog?.Invoke($"Finished with code {exitCode} (Error): Copy {source} to {target}" + Environment.NewLine); break;
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
