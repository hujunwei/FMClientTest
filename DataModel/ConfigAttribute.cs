using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModel
{
    public class ConfigAttribute
    {

        [Key]
        [Column(Order = 0)]
        public int ConfigurationId { get; set; }

        [Key]
        [Column(Order = 1)]
        public int AttributeId { get; set; }

        [Key]
        [Column(Order = 2)]
        public int Version { get; set; }

        public string AttributeValue { get; set; }

    }
}
