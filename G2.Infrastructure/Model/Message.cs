using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2.Infrastructure.Model
{
    [Table("Message", Schema ="g2")]
    public class Message
    {
        [Key]
        public long Id { get; set; }
        public long UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public long? JobId { get; set; }
        [ForeignKey(nameof(JobId))]
        public Job Job { get; set; }
        public long? TransactionId { get; set; }
        [ForeignKey(nameof(TransactionId))]
        public Transaction Transaction { get; set; }
        public bool Unread { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}