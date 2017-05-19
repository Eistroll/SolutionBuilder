﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace SolutionBuilder
{
    public class Model : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
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
            return this.SolutionObjects.Count == toCompareWith.SolutionObjects.Count &&
                this.SolutionObjects.SequenceEqual(toCompareWith.SolutionObjects);
        }

        public ObservableCollection<SolutionObject> SolutionObjects = new ObservableCollection<SolutionObject>();
        public void Save()
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(Model));
            FileStream writer = new FileStream("DataModel.xml", FileMode.Create);
            serializer.WriteObject(writer, this);
            writer.Close();
        }
        public static Model Load()
        {
            FileStream stream = new FileStream("DataModel.xml", FileMode.Open);
            XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(stream, new XmlDictionaryReaderQuotas());
            DataContractSerializer serializer = new DataContractSerializer(typeof(Model));
            Model model = (Model)serializer.ReadObject(reader, true);
            reader.Close();
            return model;
        }
    }
}
