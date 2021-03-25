using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SolutionBuilder
{

    class Builder
    {
        public string BaseDir;
        public string BaseOptions;
        public FileInfo BuildExe;
        public FileInfo PublishExe;
        public ICollection<SolutionObjectView> solutions;
        public Action<string> AddToLog;
        public Action<int, int, int, string, bool> UpdateProgress;
        public bool Build(CancellationToken token, ref int currentProcessId)
        {
            bool atLeastOneBuildFailed = false;
            if (solutions.Count == 0)
                return false;
            //(FindResource("showMe") as Storyboard).Begin();
            int count = 0;
            // Initially update with real solution count to handle single solution progress
            UpdateProgress?.Invoke(0, solutions.Count, 0, "", atLeastOneBuildFailed);
            int doubleSolutionCount = solutions.Count * 2;
            foreach (SolutionObjectView solution in solutions)
            {
                string solutionPath = BaseDir + "\\" + solution.Name;
                UpdateProgress?.Invoke(0, doubleSolutionCount, ++count, solutionPath, atLeastOneBuildFailed);
                bool failure = BuildSolution(BuildExe, solutionPath, BaseOptions, solution, ref currentProcessId);
                atLeastOneBuildFailed = atLeastOneBuildFailed || failure;
                UpdateProgress?.Invoke(0, doubleSolutionCount, ++count, solutionPath, atLeastOneBuildFailed);
                AddToLog?.Invoke(solutionPath + (failure ? " failed" : " successful") + Environment.NewLine);
                if (token.IsCancellationRequested == true)
                {
                    AddToLog?.Invoke("Build has been canceled.");
                    UpdateProgress?.Invoke(0, solutions.Count+1, solutions.Count+1, "Build has been canceled.", atLeastOneBuildFailed);
                    //token.ThrowIfCancellationRequested();
                    return false;
                }
            }
            UpdateProgress?.Invoke(0, solutions.Count, solutions.Count, "", atLeastOneBuildFailed);
            return atLeastOneBuildFailed;
        }
        private void BuildOutputHandler(object sender, DataReceivedEventArgs e, SolutionObjectView solution)
        {
            string line = e.Data + Environment.NewLine;
            solution.BuildLog += line;
        }
        private bool BuildSolution(FileInfo buildExe, string solutionPath, string baseOptions, SolutionObjectView solution, ref int currentProcessId)
        {
            bool buildFailure = true;
            solution.BuildLog = "";
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo()
            { WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            startInfo.FileName = buildExe.ToString();
            startInfo.Arguments = baseOptions + " " + solution.Options + " " + solutionPath;
            process.StartInfo = startInfo;
            // Set event handler
            process.OutputDataReceived += (s, eventargs) => BuildOutputHandler(s, eventargs, solution);
            bool Success = process.Start();
            currentProcessId = process.Id;
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
            if( solution.SuccessFlag)
            {
	            PerformPostbuildStep(solution, ref currentProcessId);
	            PerformPublish(solution, ref currentProcessId);
            }
            return buildFailure;
        }
        private int PerformPostbuildStep( SolutionObjectView solution, ref int currentProcessId )
        {
            if (solution.PostBuildStep == null || solution.PostBuildStep.Length == 0)
                return -1;
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo()
            { WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = BaseDir;
            string command = solution.PostBuildStep;
            command = command.Replace(@"{Name}", solution.Name);
            command = command.Replace(@"{BaseDir}", BaseDir);

            startInfo.Arguments = @"/c " + command;
            process.StartInfo = startInfo;
            bool Success = process.Start();
            currentProcessId = process.Id;
            process.BeginOutputReadLine();
            process.WaitForExit();
            string strLog = "PostBuild: " + command + " finished with " + process.ExitCode;
            solution.BuildLog += strLog;
            AddToLog?.Invoke(strLog + Environment.NewLine);
            return process.ExitCode;
        }
        private int PerformPublish( SolutionObjectView solution, ref int currentProcessId )
        {
	        if (string.IsNullOrEmpty(solution.SelectedPublishTo))
		        return -1;
            Regex rx = new Regex("\\/OUT:\"(.*?)\"");
            var allOutFolders = rx.Matches(solution.BuildLog);
            var outFolder = "";
            foreach (Match outFolderMatch in allOutFolders)
            {
	            if(outFolderMatch.Value.Contains("Test") || outFolderMatch.Groups.Count != 2)
                    continue;
                FileInfo info = new FileInfo(outFolderMatch.Groups[1].Value);
	            outFolder = info.DirectoryName;
				solution.LastOutFolder = outFolder;
            }

            var target = new FileInfo(solution.SelectedPublishTo).FullName;
            if (string.IsNullOrEmpty(solution.LastOutFolder) || string.IsNullOrEmpty(target))
	            return -1;
            string options = "/MIR";
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo()
                { WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardOutput = false, UseShellExecute = false, CreateNoWindow = true };
                startInfo.FileName = @PublishExe.FullName;
                startInfo.Arguments = $"\"{solution.LastOutFolder}\" \"{target}\" \"{options}\"";
                process.StartInfo = startInfo;
                bool started = process.Start();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                switch (exitCode)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3: AddToLog?.Invoke($"Finished with code {exitCode} (Success): Copy\n{outFolder} -> {solution.SelectedPublishTo}" + Environment.NewLine); break;
                    case 4:
                    case 5:
                    case 6:
                    case 7: AddToLog?.Invoke($"Finished with code {exitCode} (Warning): Copy\n{outFolder} -> {solution.SelectedPublishTo}" + Environment.NewLine); break;
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15: AddToLog?.Invoke($"Finished with code {exitCode} (Error): Copy\n{outFolder} ->  to {solution.SelectedPublishTo}" + Environment.NewLine); break;
                    case 16: AddToLog?.Invoke($"Finished with code {exitCode}: did not run" + Environment.NewLine); break;
                }
		        return exitCode;
            }
            catch (System.Exception ex)
            {
                AddToLog(ex.Message + Environment.NewLine);
            }

            return -1;
        }
    }
}
