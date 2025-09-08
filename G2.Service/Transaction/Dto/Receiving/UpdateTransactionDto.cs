namespace G2.Service.Transaction.Dto.Receiving
{
    public class UpdateErcaspayTransactionDto
    {
        public double? Amount { get; set; }
        public string? Transaction_Reference { get; set; }
        public string? Payment_Reference { get; set; }
        public string? Type { get; set; }
        public string? Service_Type { get; set; }
        public string? Fee_Bearer { get; set; }
        public string? Channel { get; set; }
        public string? Status { get; set; }
        public string? Currency { get; set; }
        public string? Merchant_Name { get; set; }
        public string? Customer_Account_Name { get; set; }
        public string? Metadata { get; set; }
    }
}