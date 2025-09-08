using System.ComponentModel.DataAnnotations.Schema;

namespace G2.Infrastructure.Model
{
    [Table("Referral", Schema ="g2")]
    public class Referral
    {
        public long Id { get; set; }
        public long ReferrerId { get; set; }
        [ForeignKey(nameof(ReferrerId))]
        public User Referrer { get; set; }
        public long ReferredId { get; set; }
        [ForeignKey(nameof(ReferredId))]
        public User Referred { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}