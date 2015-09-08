using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModel
{
    public class ConfigTemplate
    {
        public ConfigTemplate()
        {
            Active = true;
            Qualifiers = new List<Qualifier>();
            Attributes = new List<Attribute>();
        }
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        [NotMapped]
        public List<int> QualifierIds { get; set; }
        [NotMapped]
        public List<int> AttributeIds { get; set; }
        public virtual IList<Qualifier> Qualifiers { get; set; }
        public virtual IList<Attribute> Attributes { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
