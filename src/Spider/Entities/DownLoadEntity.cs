using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Entities
{
    [Table("downloads")]
    public class DownLoadEntity
    {
        public DownLoadEntity()
        {
            Id = Guid.NewGuid();
        }

        [Key]
        public Guid Id { get; set; }
        public Guid Movie { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public string Quality { get; set; }
        public string Address { get; set; }
    }
}
