using G2.Infrastructure.Model;

namespace G2.Infrastructure.TechnologyDetector
{
    public interface IWappalyzer
    {
        Task<WappalyzerResult?> Detect(string url);
    }
}