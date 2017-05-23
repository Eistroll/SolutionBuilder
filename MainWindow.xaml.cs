using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SolutionBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Model _Model = new Model();
        private MainViewModel _ViewModel = new MainViewModel();
        public MainWindow()
        {
            InitializeComponent();
            _Model = Model.Load();
            _ViewModel = MainViewModel.Load();
       }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = _ViewModel;
            _ViewModel.BindToModel( ref _Model);
        }
        private void SaveCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void SaveCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _ViewModel.Save();
            _Model.Save();
        }
        private void LoadCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void LoadCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _ViewModel = MainViewModel.Load();
            _Model = Model.Load();
            this.DataContext = _ViewModel;
            _ViewModel.BindToModel(ref _Model);
        }
        private void cbPlatform_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {

        }

        private void btBuild_Click(object sender, RoutedEventArgs e)
        {
            foreach ( SolutionObjectView solution in _ViewModel.Solutions )
            {
                if ( solution.Selected )
                {
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.UseShellExecute = false;
                    startInfo.CreateNoWindow = true;
                    startInfo.FileName = @"C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe";
                    StringBuilder path = new StringBuilder(_ViewModel.BaseDir);
                    path.Append("\\" + solution.Name);
                    startInfo.Arguments = solution.Options + " " + path;
                    process.StartInfo = startInfo;
                    bool Success = process.Start();
                    while (!process.StandardOutput.EndOfStream)
                    {
                        textBox.AppendText(process.StandardOutput.ReadLine());
                    }
                    process.WaitForExit();
                    int exitCode = process.ExitCode;
                }
            }
        }
    }
}
