// ========================================
// Security Services
// ========================================

// Booksy.UserManagement.Infrastructure/Services/Security/PasswordHasher.cs
using Booksy.UserManagement.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Infrastructure.Services.Security
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _secretKey;
        private readonly SymmetricSecurityKey _signingKey;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
            _issuer = _configuration["Jwt:Issuer"] ?? "Booksy";
            _audience = _configuration["Jwt:Audience"] ?? "Booksy.Users";
            _secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured");
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        }

        public string GenerateAccessToken(
            UserId userId,
            UserType userType,
            Email email,
            string displayName,
            string status,
            IEnumerable<string> roles,
            string? providerId = null,
            string? providerStatus = null,
            string? customerId = null,
            int expirationHours = 24)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new(ClaimTypes.Email, email.Value),
                new(ClaimTypes.Name, displayName),
                new("user_type", userType.ToString()),
                new("user-status", status),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Add providerId claim if user is a provider
            if (!string.IsNullOrEmpty(providerId))
            {
                claims.Add(new Claim("providerId", providerId));
            }

            // Add provider_status claim if available
            if (!string.IsNullOrEmpty(providerStatus))
            {
                claims.Add(new Claim("provider_status", providerStatus));
            }

            // Add customerId claim if user is a customer
            if (!string.IsNullOrEmpty(customerId))
            {
                claims.Add(new Claim("customerId", customerId));
            }

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey,
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public string? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public DateTime? GetTokenExpiration(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(token);
                return jwt.ValidTo;
            }
            catch
            {
                return null;
            }
        }

        public bool IsTokenExpired(string token)
        {
            var expiration = GetTokenExpiration(token);
            return expiration == null || expiration.Value < DateTime.UtcNow;
        }

      
    }
}

