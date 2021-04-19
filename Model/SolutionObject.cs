﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace SolutionBuilder
{
	[DataContract]
	public class SolutionObject : ICloneable
	{
		public enum Version { Initial, BuildWithPublish }
		[DataMember]
		private String ContractVersion;

		[DataMember]
		public String Name
		{
			get => _Name;
			set
			{
				if (value != null)
				{
					_Name = value;
				}
			}
		}

		[DataMember]
		public Dictionary<string, string> Options = new Dictionary<string, string>();
		[DataMember]
		public Dictionary<string, string> PostBuildSteps = new Dictionary<string, string>();
		[DataMember]
		public Dictionary<string, ObservableCollection<string>> PublishToLists = new Dictionary<string, ObservableCollection<string>>();
		[DataMember]
		public Dictionary<string, string> PublishTos = new Dictionary<string, string>();

		private string _Name;

		[DataMember]
		public String PostBuildStep { get; set; }
		// Constructor
		public SolutionObject()
		{
			ContractVersion = Version.Initial.ToString();
			Options["Release"] = "/p:Configuration=Release";
			Options["Debug"] = "/p:Configuration=Debug";
			PublishToLists["Release"] = new ObservableCollection<string>() { "" };
			PublishToLists["Debug"] = new ObservableCollection<string>() { "" };
			PublishTos["Release"] = "";
			PublishTos["Debug"] = "";
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		public override bool Equals(object obj)
		{
			var toCompareWith = obj as SolutionObject;
			if (toCompareWith == null)
				return false;
			return this.Name == toCompareWith.Name &&
				this.PostBuildStep == toCompareWith.PostBuildStep &&
				this.PostBuildSteps.OrderBy(kvp => kvp.Key).SequenceEqual(toCompareWith.PostBuildSteps.OrderBy(kvp => kvp.Key)) &&
				this.Options.OrderBy(kvp => kvp.Key).SequenceEqual(toCompareWith.Options.OrderBy(kvp => kvp.Key));
		}

		public object Clone()
		{
			return MemberwiseClone();
		}
		[OnDeserialized()]
		private void OnDeserializedMethod(StreamingContext context)
		{
			if (ContractVersion != Version.Initial.ToString() && PostBuildSteps == null)
			{
				PostBuildSteps = new Dictionary<string, string>();
				PostBuildSteps["Release"] = PostBuildStep;
				PostBuildSteps["Debug"] = PostBuildStep;
			}
			if (ContractVersion != Version.BuildWithPublish.ToString())
			{
				if (PublishToLists == null)
				{
					PublishToLists = new Dictionary<string, ObservableCollection<string>>
					{
						["Release"] = new ObservableCollection<string>() {""},
						["Debug"] = new ObservableCollection<string>() {""}
					};
				}
				if (PublishTos == null)
				{
					PublishTos = new Dictionary<string, string> {["Release"] = "", ["Debug"] = ""};
				}
			}
		}
	}
}
