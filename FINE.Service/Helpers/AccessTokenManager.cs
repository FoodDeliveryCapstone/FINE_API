using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.Helpers
{
    public class AccessTokenManager
    {
        public static string GenerateJwtToken(string name, string[] roles, Guid? id, IConfiguration configuration)
        {
            var tokenConfig = configuration.GetSection("Token");
            var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tokenConfig["SecretKey"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var permClaims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            };
            if (roles != null && roles.Length > 0)
            {
                foreach (string role in roles)
                {
                    permClaims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
            var token = new JwtSecurityToken(tokenConfig["Issuer"],
                tokenConfig["Issuer"],
                permClaims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
  