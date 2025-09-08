namespace G2.Service.Transaction.Dto.Transfer
{
    public class TransactionDto
    {
        public long Id { get; set; }
        public string Reference { get; set; }
        public string ProviderReference { get; set; }
        public double Amount { get; set; }
        public long PlanId { get; set; }
        public string PlanName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}