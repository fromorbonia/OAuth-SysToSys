using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace OAUthSysToSys
{
    public static class PrivateKeyJWTHandler
    {
       

        public static string ClientAuthJwtCreate(string TokenEndpoint,
            string ClientId,
            string PrivateKeyPFXFile)
        {
            //Example taken from:  https://www.scottbrady91.com/OAuth/Removing-Shared-Secrets-for-OAuth-Client-Authentication

            // set exp to 5 minutes
            var tokenHandler = new JwtSecurityTokenHandler { TokenLifetimeInMinutes = 5 };

            var securityToken = tokenHandler.CreateJwtSecurityToken(
                // iss must be the client_id of our application
                issuer: ClientId,
                // aud must be the identity provider (token endpoint)
                audience: TokenEndpoint,          
                // sub must be the client_id of our application
                subject: new ClaimsIdentity(
                  new List<Claim> { 
                      new Claim("sub", ClientId),
                      new Claim("jti", Guid.NewGuid().ToString()) //jti is required in the OIDC standard
                  }),
                // sign with the private key (using RS256 for IdentityServer)
                signingCredentials: new SigningCredentials(
                  new X509SecurityKey(new X509Certificate2(PrivateKeyPFXFile)), "RS256")
            );

            return tokenHandler.WriteToken(securityToken);
        }

        public static string JWKSGenerate(string PublicKeyCERFile)
        {
            //JsonWebKeySet jwks = new JsonWebKeySet();
            JsonWebKey jwk = JsonWebKeyConverter.ConvertFromX509SecurityKey(
                new X509SecurityKey(new X509Certificate2(PublicKeyCERFile)));

            //jwks.Keys.Add(jwk);
            
            //var keys = new[] { jwk };

            JsonSerializerOptions jso = new JsonSerializerOptions() {
                IgnoreNullValues = true
            };
            string jwksStr = string.Format("{{ keys: [{0}] }}",  JsonSerializer.Serialize(jwk, jso));
            return  jwksStr;
        }
    }

    public class JWKSPublicEndpoint : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path == "/.well-known/jwks.json")
            {
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(PrivateKeyJWTHandler.JWKSGenerate("./testkeypair.cer"));
            }
            else
            {
                await next(context);
            }

        }
    }

    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseJWKSPublicEndpointMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JWKSPublicEndpoint>();
        }
    }

}
