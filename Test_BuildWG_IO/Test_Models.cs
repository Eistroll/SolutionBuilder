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
            Model.SolutionObjects.Add(new SolutionObject
            {
                Name = "BCGCBPro140.sln"
                    ,
                Options = new Dictionary<string, string> { { "Release", "/p:Configuration=\"Unicode Release\"" }, { "Debug", "/p:Configuration=\"Unicode Debug\"" } }
            });
            Assert.AreEqual(0, ViewModel.Solutions.Count);
            ViewModel.BindToModel(ref Model);
            Assert.AreEqual(1, ViewModel.Solutions.Count);
            Model.SolutionObjects.Add(new SolutionObject
            {
                Name = "zlib140.sln"
                   ,
                Options = new Dictionary<string, string> { { "Release", "/p:Configuration=\"Unicode Release\"" }, { "Debug", "/p:Configuration=\"Unicode Debug\"" } }
            });
            Assert.AreEqual(1, ViewModel.Solutions.Count);
        }
    }
}
