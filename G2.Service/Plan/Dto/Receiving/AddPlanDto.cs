namespace G2.Service.Plan.Dto.Receiving
{
    public class AddPlanDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public double Discount { get; set; }
        public bool Visualisation { get; set; }
        public bool Screenshot { get; set; }
        public bool AIReport { get; set; }
    }
}