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
            model.SolutionObjects.Add(new SolutionObject
            {
                Name = "BCGCBPro140.sln"
                    ,
                Options = new Dictionary<string, string> { { "Release", "/p:Configuration=\"Unicode Release\"" }, { "Debug", "/p:Configuration=\"Unicode Debug\"" } }
            });
            Assert.AreEqual(1, model.SolutionObjects.Count);
            model.Save();
            Model loadedModel = Model.Load();
            CollectionAssert.AreEqual(model.SolutionObjects, loadedModel.SolutionObjects);
        }

        [TestMethod]
        public void ViewModelSave()
        {
            Model model = new Model();
            model.SolutionObjects.Add(new SolutionObject
            {
                Name = "BCGCBPro140.sln"
                    ,
                Options = new Dictionary<string, string> { { "Release", "/p:Configuration=\"Unicode Release\"" }, { "Debug", "/p:Configuration=\"Unicode Debug\"" } }
            });
            model.SolutionObjects.Add(new SolutionObject
            {
                Name = "DemoPanel.sln"
                    ,
                Options = new Dictionary<string, string> { { "Release", "/p:Configuration=\"Unicode Release\"" }, { "Debug", "/p:Configuration=\"Unicode Debug\"" } }
            });
            MainViewModel viewModel = new MainViewModel();
            viewModel.BindToModel(ref model);
            Assert.AreEqual(2, model.SolutionObjects.Count);
            Assert.AreEqual(2, viewModel.Solutions.Count);
            model.Save();
            MainViewModel loadedModel = MainViewModel.Load();
            CollectionAssert.AreEqual(viewModel.Paths, loadedModel.Paths);
            CollectionAssert.AreEqual(viewModel.Platforms, loadedModel.Platforms);
            Assert.AreEqual(viewModel.SelectedPath, loadedModel.SelectedPath);
        }

    }
}
