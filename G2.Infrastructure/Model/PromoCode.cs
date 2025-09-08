using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2.Infrastructure.Model
{
    [Table("PromoCode", Schema ="g2")]
    public class PromoCode
    {
        [Key]
        public long Id { get; set; }
        public string Code { get; set; }
        public double Discount { get; set; }
        public long UsageLimit { get; set; }
        public long UsageCount { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}