namespace G2.Service.PromoCode.Dto.Receiving
{
    public class VerifyPromoCodeDto
    {
        public string Code { get; set; }
        public long PlanId { get; set; }
        public bool IsYearly { get; set; }
    }
}