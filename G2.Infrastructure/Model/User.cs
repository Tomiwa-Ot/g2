using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2.Infrastructure.Model
{
    [Table("User", Schema ="g2")]
    public class User
    {
        [Key]
        public long Id { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? Salt { get; set; }
        public long RoleId { get; set; }
        [ForeignKey(nameof(RoleId))]
        public Role Role { get; set; }
        public string? Provider { get; set; }
        public string ReferralCode { get; set; }
        public long ReferralQuota { get; set; }
        public string AuthToken { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiration { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiration { get; set; }
        public bool IsVerified { get; set; }
        public bool IsDisabled { get; set; }
        public long PlanId { get; set; }
        [ForeignKey(nameof(PlanId))]
        public Plan Plan { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? PlanExpiration { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}