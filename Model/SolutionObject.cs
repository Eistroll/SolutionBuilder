using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace SolutionBuilder
{
    [DataContract]
    public class SolutionObject
    {
        public SolutionObject()
        {
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
                this.BaseDir == toCompareWith.BaseDir &&
                this.RelativePath == toCompareWith.RelativePath &&
                this.Options.OrderBy(kvp=>kvp.Key).SequenceEqual(toCompareWith.Options.OrderBy(kvp=>kvp.Key));
        }
        [DataMember]
        public String BaseDir { get; set; }
        [DataMember]
        public String Name { get; set; }
        [DataMember]
        public String RelativePath { get; set; }
        [DataMember]
        public Dictionary<string, string> Options = new Dictionary<string, string>();
    }
}
