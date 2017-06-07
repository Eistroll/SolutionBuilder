using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolutionBuilder;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

namespace Test_SolutionBuilder
{
    [TestClass]
    public class Test_XML
    {
        [TestMethod]
        public void ModelSave()
        {
            Model model = new Model();
            model.Scope2SolutionObjects["test"] = new System.Collections.ObjectModel.ObservableCollection<SolutionObject>();
            model.Scope2SolutionObjects["test"].Add(new SolutionObject
            {
                Name = "BCGCBPro140.sln"
                    ,
                Options = new Dictionary<string, string> { { "Release", "/p:Configuration=\"Unicode Release\"" }, { "Debug", "/p:Configuration=\"Unicode Debug\"" } }
            });
            model.Save();
            Model loadedModel = Model.Load();
            Assert.AreEqual(loadedModel.Scope2SolutionObjects.Count, model.Scope2SolutionObjects.Count);
            Assert.AreEqual(1, loadedModel.Scope2SolutionObjects.Count);
            CollectionAssert.AreEqual(model.Scope2SolutionObjects["test"], loadedModel.Scope2SolutionObjects["test"]);
        }

        [TestMethod]
        public void ViewModelSave()
        {
            Model model = new Model();
            model.Scope2SolutionObjects["test"] = new System.Collections.ObjectModel.ObservableCollection<SolutionObject>();
            model.Scope2SolutionObjects["test"].Add(new SolutionObject
            {
                Name = "BCGCBPro140.sln"
                    ,
                Options = new Dictionary<string, string> { { "Release", "/p:Configuration=\"Unicode Release\"" }, { "Debug", "/p:Configuration=\"Unicode Debug\"" } }
            });
            model.Scope2SolutionObjects["test"].Add(new SolutionObject
            {
                Name = "DemoPanel.sln"
                    ,
                Options = new Dictionary<string, string> { { "Release", "/p:Configuration=\"Unicode Release\"" }, { "Debug", "/p:Configuration=\"Unicode Debug\"" } }
            });
            MainViewModel viewModel = new MainViewModel();
            viewModel.Tabs.Add(new TabItem() { Header = "test" });
            viewModel.BindToModel(ref model);
            var tab = viewModel.Tabs[0];
            tab.Solutions[1].Selected = true;
            Assert.AreEqual(2, model.Scope2SolutionObjects["test"].Count);
            Assert.AreEqual(2, tab.Solutions.Count);
            model.Save();
            MainViewModel loadedModel = MainViewModel.Load();
            Assert.AreEqual(1, loadedModel.Tabs.Count);
            var loadedTab = loadedModel.Tabs[0];
            CollectionAssert.AreEqual(tab.Platforms, loadedTab.Platforms);
            Assert.AreEqual(tab.SelectedPath, loadedTab.SelectedPath);
            Assert.IsFalse(tab.Solutions[0].Selected);
            Assert.IsTrue(tab.Solutions[1].Selected);
        }

    }
}
