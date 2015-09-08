using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModel
{
    public class Category
    {
        public Category()
        {
            Active = true;
        }
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        [Index("IX_CategoryName", IsUnique = true)]
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
