using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PetFlowAPI.Models;

namespace PetFlowAPI.Security;

public sealed class JwtOptions
{
    public string Key { get; set; } = "development-only-change-this-key-please-32-chars";
    public string Issuer { get; set; } = "PetFlowAPI";
    public string Audience { get; set; } = "PetFlowClients";
    public int ExpirationMinutes { get; set; } = 60;
}

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}

public sealed class PasswordService : IPasswordService
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string storedHash)
    {
        try
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2-SHA256") return false;
            var iterations = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
        catch (ArgumentException) { return false; }
    }
}

public interface ITokenService
{
    string CreateToken(Tutor tutor);
}

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public string CreateToken(Tutor tutor)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, tutor.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, tutor.Id.ToString()),
            new Claim(ClaimTypes.Name, tutor.Name),
            new Claim(ClaimTypes.Email, tutor.Email),
            new Claim(ClaimTypes.Role, tutor.Role)
        };
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
