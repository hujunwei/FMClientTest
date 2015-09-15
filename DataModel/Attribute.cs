using System;

namespace DataModel
{
    public class Attribute
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public AttributeType AttributeType { get; set; }
        public string Rule { get; set; }
        public bool Required { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
