

namespace ApiGateway.Auth.Jwt
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Authority { get; set; } = string.Empty;
        public string MetadataAddress {  get; set; } = string.Empty;
        public string ValidIssuer {  get; set; } = string.Empty;
        public string Audience {  get; set; } = string.Empty;
        public bool RequireHttpsMetadata { get; set; } = false;
    }
}
