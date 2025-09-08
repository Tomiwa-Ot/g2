namespace G2.Service.Messages.Dto.Transfer
{
    public class MessageDto
    {
        public long Id { get; set; }
        public long? JobId { get; set; }
        public long? TransactionId { get; set; }
        public long UserId { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public bool Unread { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}