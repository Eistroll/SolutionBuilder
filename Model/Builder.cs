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
    public class Builder
    {
        private MainViewModel _ViewModel;
        public Builder(MainViewModel viewModel)
        {
            _ViewModel = viewModel;
        }
        private void BuildOutputHandler(object sender, DataReceivedEventArgs e, SolutionObjectView solution)
        {
            string line = e.Data + Environment.NewLine;
            solution.BuildLog += line;
        }
        public bool BuildSolutions(TabItem tab, FileInfo buildExe, ObservableCollection<SolutionObjectView> solutions = null, Action<string> AddToLog = null)
        {
            bool buildFailure = false;
            bool ignoreSelection = true;
            if (solutions == null)
            {
                solutions = tab.Solutions;
                ignoreSelection = false;
            }
            foreach (SolutionObjectView solution in solutions)
            {
                if (ignoreSelection || solution.Selected)
                {
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo()
                    { WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                    startInfo.FileName = buildExe.ToString();
                    StringBuilder path = new StringBuilder(_ViewModel.GetSetting("BaseDir", tab.Header));
                    path.Append("\\" + solution.Name);
                    startInfo.Arguments = tab.BaseOptions + " " + solution.Options + " " + path;
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

                    AddToLog?.Invoke(path + (buildFailure ? " failed" : " successful") + Environment.NewLine);
                    solution.SuccessFlag = exitCode == 0 ? true : false;
                }
            }
            return buildFailure;
        }
    }
}
