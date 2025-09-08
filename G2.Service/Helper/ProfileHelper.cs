using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;

namespace G2.Service.Helper
{
    public class ProfileHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProfileHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GenerateReferralCode(int length)
        {
            return Guid.NewGuid().ToString()[..length];
        }

        public (long? Id, string? Email, string? Role) GetUserDetails()
        {
            string? token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(token) || !token.StartsWith("Bearer ")) return (null, null, null);

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwt = handler.ReadJwtToken(token["Bearer ".Length..]);
            IEnumerable<Claim> claims = jwt.Claims;

            int id = int.Parse(claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value);
            string email = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            string role = claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;

            return (id, email, role);
        }

        public (byte[], string) HashPassword(byte[] salt, string password)
        {
            // generate a 128-bit salt using a cryptographically strong random sequence of nonzero values
            if (salt.Length == 0)
            {
                salt = new byte[128 / 8];
                using (var rngCsp = RandomNumberGenerator.Create())
                {
                    rngCsp.GetNonZeroBytes(salt);
                }
            }

            // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return (salt, hashed);
        }

        public bool VerifyPassword(string expectedHash, string salt, string password)
        {
             // generate hash of provided password
            var saltBytes = Convert.FromBase64String(salt);
            var (_, hash) = HashPassword(saltBytes, password);

            // compare stored hash to provided hash
            return expectedHash.Equals(hash);
        }
    }
}