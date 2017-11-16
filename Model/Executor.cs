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
    public class Executor
    {
        CancellationTokenSource cancelTokenSource;
        CancellationToken token = CancellationToken.None;
        private MainViewModel _ViewModel;
        public Executor(MainViewModel viewModel)
        {
            _ViewModel = viewModel;
            cancelTokenSource = new CancellationTokenSource();
            token = cancelTokenSource.Token;
        }
        public Task Execute(Action action)
        {
            return Task.Run(() => action());
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
        }
    }
}
