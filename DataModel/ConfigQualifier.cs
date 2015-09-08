using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModel
{
    public class ConfigQualifier
    {
        [Key]
        [Column(Order = 0)]
        public int ConfigurationId { get; set; }
        [Key]
        [Column(Order = 1)]
        public int QualifierId { get; set; }
        public string QualifierValue { get; set; }
    }
}
