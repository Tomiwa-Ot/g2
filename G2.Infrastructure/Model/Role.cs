using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2.Infrastructure.Model
{
    [Table("Role", Schema ="g2")]
    public class Role
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}