using SolutionBuilder.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace SolutionBuilder
{
    public class Executor : IDisposable
    {
        CancellationTokenSource cancelTokenSource;
        CancellationToken token = CancellationToken.None;
        public int CurrentProcessId = 0;
        private MainViewModel _ViewModel;
        public Executor(MainViewModel viewModel)
        {
            _ViewModel = viewModel;
            cancelTokenSource = new CancellationTokenSource();
            token = cancelTokenSource.Token;
        }
        ~Executor()
        {
            Dispose(false);
        }
        public Task Execute( Action<CancellationToken> action )
        {
            return Task.Run(() => action(token), token);
        }
        public void Cancel()
        {
            cancelTokenSource.Cancel();
            cancelTokenSource.Dispose();
            cancelTokenSource = new CancellationTokenSource();
            token = cancelTokenSource.Token;
            if( CurrentProcessId > 0)
            {
                Process[] process = Process.GetProcesses();
                foreach (Process prs in process)
                {
                    if (prs.Id == CurrentProcessId)
                    {
                        prs.Kill();
                        break;
                    }
                }
            }

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((IDisposable)cancelTokenSource).Dispose();
            }
        }
    }
}
