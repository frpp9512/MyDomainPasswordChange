namespace MyDomainPasswordChange.Authentication.Configuration;

public record JwtGeneratorOptions
{
    public required string Secret { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int Expires { get; set; } = 30;
    public Dictionary<string, string> ExtraClaims { get; set; } = [];
}
