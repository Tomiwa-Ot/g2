namespace G2.Service.Transaction.Dto.Receiving
{
    public class VerifyFlutterwave
    {
        public string Status { get; set;  }
        public string TransactionReference { get; set; }
        public long TransactionId { get; set;  }
    }
}