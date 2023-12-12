namespace MyDomainPasswordChange.Shared.DTO;

public record ErrorResponseDto(int StatusCode, string ErrorCode, string ErrorMessage, Dictionary<string, object> Data);
