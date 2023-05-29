#region using section

using System;

#endregion

namespace AFMaster.Util
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Method : Attribute
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Id { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class Parameter : Attribute
    {
        public Parameter(string description)
        {
            Description = description;
        }

        public string Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Id { get; set; }
    }
}