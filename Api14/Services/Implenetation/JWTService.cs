using Api14.CORE;
using Api14.Models;
using Api14.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api14.Services.Implenetation
{
    public class JWTService : IJWTService
    {
        public UserToken GetUserToken(User user)
        {
            var jwtKey = "ea9386ec9175b2c35d834ce85acdb7ed34d5bff3ec47bb8f5340987d64c5b14ff46285f8f221ca95324847da5e9c1d82e00866532912eb904fdc353748a9db24d7b615750b1b3c39a8ca4bb98f5383dce76876fa947d368a37cba19f45b63430d72eb54eebbd5ecea3ac88a18e755bef08680ae6b80a41483c296c007bc82b61464965d690291a177cbd6f21432486711855c82ac547ea4bee82b0071c2eb37db497704f9814d6a69df52dd6c4de70a554f55921ce93f7e75396f694c679a56a7b40e04c9c93e535de144c6fa3af35920d6bfe530f94302a7f1cd069138b29627d36a6ab985b23976c1f25c401141a4db26826c6a67b031d25a5ccfd9711f6e2";
            var jwtIssuer = "chven";
            var jwtAudience = "isini";
            var jwtDuration = 300;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                expires: DateTime.Now.AddMinutes(jwtDuration),
                claims: claims,
                signingCredentials: credentials
            );


            return new UserToken
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };

        }
    }
}
