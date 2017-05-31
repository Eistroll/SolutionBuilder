using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolutionBuilder;
using System.Collections.Generic;

namespace Test_SolutionBuilder
{
    [TestClass]
    public class Test_Models
    {
        [TestMethod]
        public void ModelBinding()
        {
            MainViewModel ViewModel = new MainViewModel();
            Model Model = new Model();
            Model.Scope2SolutionObjects["test"] = new System.Collections.ObjectModel.ObservableCollection<SolutionObject>();
            Model.Scope2SolutionObjects["test"].Add(new SolutionObject
            {
                Name = "BCGCBPro140.sln"
                    ,
                Options = new Dictionary<string, string> { { "Release", "/p:Configuration=\"Unicode Release\"" }, { "Debug", "/p:Configuration=\"Unicode Debug\"" } }
            });
            Assert.AreEqual(0, ViewModel.Tabs.Count);
            ViewModel.BindToModel(ref Model);
            //Assert.AreEqual(1, ViewModel.Solutions.Count);
            //Model.SolutionObjects.Add(new SolutionObject
            //{
            //    Name = "zlib140.sln"
            //       ,
            //    Options = new Dictionary<string, string> { { "Release", "/p:Configuration=\"Unicode Release\"" }, { "Debug", "/p:Configuration=\"Unicode Debug\"" } }
            //});
            //Assert.AreEqual(1, ViewModel.Solutions.Count);
        }
        [TestMethod]
        public void SettingsChanges()
        {
            MainViewModel ViewModel = new MainViewModel();
            Model Model = new Model();
            ViewModel.BindToModel(ref Model);
            Assert.AreEqual(1, ViewModel.SettingsList.Count);
            ViewModel.SettingsList[0].Value = "x";
            ViewModel.SettingsList.Add(new Setting() { Scope = "scope", Key = "key", Value = "value" });
            ViewModel.SettingsList[1].Value = "valueX";
        }
    }
}
