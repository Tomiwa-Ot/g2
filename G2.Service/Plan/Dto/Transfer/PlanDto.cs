namespace G2.Service.Plan.Dto.Transfer
{
    public class PlanDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double OriginalPrice { get; set; }
        public double DiscountedPrice { get; set; }
        public double Discount { get; set; }
        public long Quota { get; set; }
        public long Concurrency { get; set; }
        public bool Screenshot { get; set; }
        public bool Visualisation { get; set; }
        public bool AIReport { get; set; }
        public bool ConsoleApp { get; set; }
    }
}
