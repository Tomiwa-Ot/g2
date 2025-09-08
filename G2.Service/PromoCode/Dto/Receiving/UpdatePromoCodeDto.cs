namespace G2.Service.PromoCode.Dto.Receiving
{
    public class UpdatePromoCodeDto
    {
        public string? Code { get; set; }
        public double? Discount { get; set; }
        public long? UsageLimit { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}