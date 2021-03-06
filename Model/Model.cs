﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace SolutionBuilder
{
    public class Model : INotifyPropertyChanged
    {
        // Properties
        public Dictionary<string, ObservableCollection<SolutionObject>> Scope2SolutionObjects = new Dictionary<string, ObservableCollection<SolutionObject>>();
        
        //Constructor
        public Model()
        {
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            var toCompareWith = obj as Model;
            if (toCompareWith == null)
                return false;
            return this.Scope2SolutionObjects.Count == toCompareWith.Scope2SolutionObjects.Count;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // IO
        public void Save()
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(Model));
            FileStream writer = new FileStream("DataModel.xml", FileMode.Create);
            if (!writer.CanWrite)
                return;
            serializer.WriteObject(writer, this);
            writer.Close();
        }
        public static Model Load()
        {
            FileInfo file = new FileInfo("DataModel.xml");
            if (!file.Exists)
                return new Model();
            FileStream stream = new FileStream(file.Name, FileMode.Open);
            XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(stream, new XmlDictionaryReaderQuotas());
            DataContractSerializer serializer = new DataContractSerializer(typeof(Model));
            Model model = (Model)serializer.ReadObject(reader, true);
            reader.Close();
            return model;
        }
    }
}
