namespace ApiGateway.Auth.Abstractions
{
    public interface ITokenValidator
    {
        Task<TokenValidatorResult> ValidateAsync(string token);
    }

    public record TokenValidatorResult(
        bool isValid,
        string? UserId, 
        string? UserName,
        IEnumerable<string> Roles,
        string? ErrorMessage
    );
}
