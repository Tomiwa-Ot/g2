namespace G2.Service.Helper
{
    public static class LoginProvider
    {
        public static readonly List<string> _providers =
        [
            "google",
            "github"
        ];

        public static bool Supported(string provider) => _providers.Contains(provider.ToLower());
    }
}