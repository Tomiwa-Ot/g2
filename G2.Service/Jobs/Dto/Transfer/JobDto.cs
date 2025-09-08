namespace G2.Service.Jobs.Dto.Transfer
{
    public class JobDto
    {
        public long Id { get; set; }
        public string Url { get; set; }
        public string? Output { get; set; }
        public string? Screenshot { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}