using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiGateway.Auth.Jwt
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Authority { get; set; } = string.Empty;
        public string Audience {  get; set; } = string.Empty;
        public bool RequireHttpsMetadata { get; set; } = false;
    }
}
