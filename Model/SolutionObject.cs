using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace SolutionBuilder
{
    [DataContract]
    public class SolutionObject
    {
        [DataMember]
        public String Name { get; set; }
        [DataMember]
        public Dictionary<string, string> Options = new Dictionary<string, string>();

        // Constructor
        public SolutionObject()
        {
            Options["Release"] = "/p:Configuration=Release";
            Options["Debug"] = "/p:Configuration=Debug";
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
                this.Options.OrderBy(kvp=>kvp.Key).SequenceEqual(toCompareWith.Options.OrderBy(kvp=>kvp.Key));
        }
    }
}
