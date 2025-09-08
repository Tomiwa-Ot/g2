namespace G2.Infrastructure.Model
{
    public class Header
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class Get
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class Post
    {
        public string Value { get; set; }
    }

    public class Technology
    {
        public string Name { get; set; }
        public string? Version { get; set; }
    }

    public class Site
    {
        public string Url { get; set; }
        public List<Header> RequestHeaders { get; set; }
        public List<Header> ResponseHeaders { get; set; }
        public List<Get> GetParams { get; set; }
        public List<Post> PostParams { get; set; }
    }
}