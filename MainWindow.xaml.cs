using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
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
        private String MSBuild_EXE = "";
        private BitmapImage ImageBuildSuccess;
        private BitmapImage ImageBuildFailure;
        public MainWindow()
        {
            InitializeComponent();
            ImageBuildFailure = new BitmapImage(new Uri(@"Images/img_delete_16.png",UriKind.Relative));
            ImageBuildSuccess = new BitmapImage(new Uri(@"Images/img_check_16.png",UriKind.Relative));
            _Model = Model.Load();
            _ViewModel = MainViewModel.Load();
            List<String> msBuildExes= new List<String>();
            try
            {
                msBuildExes = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\MSBuild", "msbuild.exe", SearchOption.AllDirectories).ToList();
            }
            catch(UnauthorizedAccessException){ MSBuild_EXE = @"C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe"; }
            finally { }
            if (msBuildExes.Count > 0)
                MSBuild_EXE = msBuildExes[0];
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
        private void mnuSettings_Click(object sender, RoutedEventArgs e)
        {
            Window settings = new SolutionBuilder.Settings();
            settings.ShowDialog();
        }
        private void btBuild_Click(object sender, RoutedEventArgs e)
        {
            if (MSBuild_EXE.Length == 0)
                return;
            foreach (SolutionObjectView solution in _ViewModel.Solutions) { solution.BuildState = null; }
            lvSolutions.InvalidateVisual();
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
                    startInfo.FileName = MSBuild_EXE;
                    StringBuilder path = new StringBuilder(_ViewModel.BaseDir);
                    path.Append("\\" + solution.Name);
                    startInfo.Arguments = _ViewModel.BaseOptions + " " + solution.Options + " " + path;
                    process.StartInfo = startInfo;
                    bool Success = process.Start();
                    while (!process.StandardOutput.EndOfStream)
                    {
                        String line = process.StandardOutput.ReadLine() + Environment.NewLine;
                        solution.BuildLog += line;
                        textBox.AppendText(line);
                    }
                    process.WaitForExit();
                    int exitCode = process.ExitCode;
                    if (exitCode == 0)
                        solution.BuildState = ImageBuildSuccess;
                    else
                        solution.BuildState = ImageBuildFailure;
                }
            }
        }
    }
}
