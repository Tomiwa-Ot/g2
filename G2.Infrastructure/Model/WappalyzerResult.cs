namespace G2.Infrastructure.Model
{
    public class WappalyzerResult
    {
        public Dictionary<string, UrlInfo> Urls { get; set; }
        public List<Tech> Technologies { get; set; }
    }

    public class UrlInfo
    {
        public long Status { get; set; }
        public string? Error { get; set; }
    }

    public class Tech
    {
        public string Slug { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public long Confidence { get; set; }
        public string? Version { get; set; }
        public string? Icon { get; set; }
        public string? Website { get; set; }
        public string? Cpe { get; set; }
        public List<Category> Categories { get; set; }
        public bool RootPath { get; set; }
    }

    public class Category
    {
        public long Id { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
    }
}