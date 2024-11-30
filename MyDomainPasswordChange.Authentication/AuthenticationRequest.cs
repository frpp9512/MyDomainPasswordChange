using System.ComponentModel.DataAnnotations;

namespace MyDomainPasswordChange.Authentication;

public record AuthenticationRequest
{
    public required string AccountName { get; set; }

    [DataType(DataType.Password)]
    public required string Password { get; set; }
}
