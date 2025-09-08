namespace G2.Service.Messages.Dto.Receiving
{
    public class AddMessageDto
    {
        public long? JobId { get; set; }
        public long? TransactionId { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }  
        public string Body { get; set; } 
    }
}