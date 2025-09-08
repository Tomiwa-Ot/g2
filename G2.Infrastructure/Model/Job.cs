using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2.Infrastructure.Model
{
    [Table("Job", Schema ="g2")]
    public class Job
    {
        [Key]
        public long Id { get; set; }
        public string Url { get; set; }
        public string? Output { get; set; }
        public string? Screenshot { get; set; }
        public string Status { get; set; }
        public long UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}