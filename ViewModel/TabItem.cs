﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SolutionBuilder
{
    [DataContract]
    public class TabItem
    {
        [DataMember]
        public string Header { get; set; }
        [DataMember]
        public String SelectedPath { get; set; }
        [DataMember]
        public String BaseOptions { get; set; }
        [DataMember]
        public StringCollection SelectedSolutions { get; set; }
        [DataMember]
        public StringCollection Paths { get; set; }
        [DataMember]
        public StringCollection Platforms { get; set; }

        [DataMember]
        private String _SelectedPlatform;
        public String SelectedPlatform
        {
            get { return _SelectedPlatform; }
            set
            {
                _SelectedPlatform = value;
                for (int i = 0; i < Solutions.Count; ++i) {
                    Solutions[i].Options = _Model.Scope2SolutionObjects[Header][i].Options[_SelectedPlatform];
                }
            }
        }
        [DataMember]
        private int _SelectedSolutionIndex;

        //////////////////////////////////////////////////////////////////////////
        //Not serialized Data members
        //////////////////////////////////////////////////////////////////////////
        public int SelectedSolutionIndex
        {
            get { return _SelectedSolutionIndex; }
            set
            {
                _SelectedSolutionIndex = value;
                if (_SelectedSolutionIndex != -1)
                    _ViewModel.UpdateLog(Solutions[_SelectedSolutionIndex]);
            }
        }
        private ObservableCollection<SolutionObjectView> _Solutions;
        public ObservableCollection<SolutionObjectView> Solutions
        {
            get { return _Solutions ?? (Solutions = new ObservableCollection<SolutionObjectView>()); }
            set { _Solutions = value; }
        }
        private StringCollection _AllSolutions;
        public StringCollection AllSolutions
        {
            get { return _AllSolutions ?? (_AllSolutions = new StringCollection()); }
            set { _AllSolutions = value; }
        }
        private MainViewModel _ViewModel;
        private Model _Model;

        public TabItem()
        {
            AllSolutions = new StringCollection();
            SelectedSolutions = new StringCollection();
            Solutions = new ObservableCollection<SolutionObjectView>();
            Paths = new StringCollection();
            Platforms = new StringCollection();
            Platforms.Add("Release");
            Platforms.Add("Debug");
            _SelectedPlatform = "Debug";
            SelectedSolutionIndex = -1;
        }
        public void UpdateAvailableSolutions()
        {
            AllSolutions.Clear();
            String BaseDir = _ViewModel.GetSetting("BaseDir", Header);
            if (BaseDir.Length == 0)
                return;
            System.IO.DirectoryInfo BaseDirInfo = new System.IO.DirectoryInfo(BaseDir);
            if (BaseDirInfo.Exists) {
                var solutionPaths = Directory.GetFiles(BaseDir, @"*.sln", SearchOption.AllDirectories);
                foreach (var path in solutionPaths) {
                    String newPath = path.Replace(BaseDir, "");
                    AllSolutions.Add(newPath);
                }
            }
        }
        public void BindToModel(ref Model Model, ref MainViewModel ViewModel)
        {
            _Model = Model;
            _ViewModel = ViewModel;
            UpdateAvailableSolutions();

            UpdateFromModel(ref Model);
        }
        private void UpdateFromModel(ref Model Model)
        {
            Solutions.Clear();
            if (Model.Scope2SolutionObjects.Count == 0 || !Model.Scope2SolutionObjects.ContainsKey(Header))
                return;
            foreach (SolutionObject solution in Model.Scope2SolutionObjects[Header]) {
                SolutionObject tmp = solution;
                SolutionObjectView solutionView = new SolutionObjectView(ref tmp, SelectedPlatform);
                if (SelectedSolutions.Contains(tmp.Name))
                    solutionView.Selected = true;
                solutionView.PropertyChanged += new PropertyChangedEventHandler(SolutionView_PropertyChanged);
                Solutions.Add(solutionView);
            }
        }
        private void SolutionView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Options") {
                SolutionObjectView solView = (SolutionObjectView)sender;
                if (solView == null)
                    return;
                if (solView.SolutionObject != null) {
                    solView.SolutionObject.Options[SelectedPlatform] = solView.Options;
                }
            }
            if (e.PropertyName == "Selected") {
                SolutionObjectView solView = (SolutionObjectView)sender;
                if (solView == null)
                    return;
                if (!solView.Selected)
                    SelectedSolutions.Remove(solView.Name);
                else
                    SelectedSolutions.Add(solView.Name);
            }
        }
        private ICommand _AddSolutionCmd;
        public ICommand AddSolutionCmd
        {
            get { return _AddSolutionCmd ?? (_AddSolutionCmd = new CommandHandler(() => AddSolution(), true)); }
        }
        public void AddSolution()
        {
            SolutionObject solution = new SolutionObject();
            if (!_Model.Scope2SolutionObjects.ContainsKey(Header)) {
                _Model.Scope2SolutionObjects[Header] = new ObservableCollection<SolutionObject>();
            }
            _Model.Scope2SolutionObjects[Header].Add(solution);
            SolutionObjectView solutionView = new SolutionObjectView(ref solution, SelectedPlatform);
            solutionView.PropertyChanged += new PropertyChangedEventHandler(SolutionView_PropertyChanged);
            Solutions.Add(solutionView);
        }
    }
}
