using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2.Infrastructure.Model
{
    [Table("Transaction", Schema = "g2")]
    public class Transaction
    {
        [Key]
        public long Id { get; set; }
        public string Provider { get; set; }
        public string Reference { get; set; }
        public string ProviderReference { get; set; }
        public bool IsYearly { get; set; }
        public long UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set;}
        public long PlanId { get; set; }
        [ForeignKey(nameof(PlanId))]
        public Plan Plan { get; set; }
        public double Amount { get; set; }
        public string Status { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}