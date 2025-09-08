namespace G2.Service.Transaction.Dto.Receiving
{
    public class AddTransactionDto
    {
        public long PlanId { get; set; }
        public bool Yearly { get; set; }
        public string Provider { get; set; }
        public string? PromoCode { get; set; }  
    }
}