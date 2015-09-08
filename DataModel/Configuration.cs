using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;


namespace DataModel
{
    public class Configuration
    {
        public Configuration()
        {
            Active = true;
            Default = false;
            AttributeList = new List<ConfigAttribute>();
            QualifierList = new List<ConfigQualifier>();
        }
        public int Id { get; set; }
        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        [ForeignKey("Template")]
        public int TemplateId { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public bool Default { get; set; }
        public virtual Category Category { get; set; }
        public virtual ConfigTemplate Template { get; set; }
        public virtual IList<ConfigAttribute> AttributeList { get; set; }
        public virtual IList<ConfigQualifier> QualifierList { get; set; } 
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
