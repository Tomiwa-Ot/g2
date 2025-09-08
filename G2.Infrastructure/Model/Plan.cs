using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2.Infrastructure.Model
{
    [Table("Plan", Schema ="g2")]
    public class Plan
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long Quota { get; set; }
        public long Concurrency { get; set; }
        public double Price { get; set; }
        public double Discount { get; set; }
        public bool Screenshot { get; set; }
        public bool Visualisation { get; set; }
        public bool AIReport { get; set; }
        public bool ConsoleApp { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}