namespace G2.Service.Transaction.Dto.Receiving
{
    public class UpdateFlutterwaveTransactionDto
    {
        public string? Event { get; set; }
        public TransactionDetails? Data { get; set; }
    }

    public class TransactionDetails
    {
        public long? Id { get; set; }
        public string? Tx_Ref { get; set; }
        public string? Flw_Ref { get; set; }
        public string? Device_Fingerprint { get; set; }
        public double? Amount { get; set; }
        public string? Currency { get; set; }
        public double? Charged_Amount { get; set; }
        public double? App_Fee { get; set; }
        public string? Processor_Response { get; set; }
        public double? Merchant_Fee { get; set; }
        public string? Auth_Model { get; set; }
        public string? Ip { get; set; }
        public string? Narration { get; set; }
        public string? Status { get; set; }
        public string? Payment_Type { get; set; }
        public DateTime? Created_At { get; set; }
        public double? Account_Id { get; set; }
        public CustomerInfo? Customer { get; set; }

        public CardInfo? Card { get; set; }
    }

    public class CustomerInfo
    {
        public long? Id { get; set; }
        public string? Name { get; set; }
        public string? Phone_Number { get; set; }
        public string? Email { get; set; }
        public DateTime? Created_At { get; set; }
    }

    public class CardInfo
    {
        public string? First_6digits { get; set; }
        public string? Last_4digits { get; set; }
        public string? Issuer { get; set; }
        public string? Country { get; set; }
        public string? Type { get; set; }
        public string? Expiry { get; set; }
    }
}