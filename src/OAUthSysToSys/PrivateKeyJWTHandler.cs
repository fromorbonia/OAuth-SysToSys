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
using Microsoft.Extensions.Configuration;

namespace OAUthSysToSys
{
    /// <summary>
    /// Class handling creating and signing a Client Authentication JWT
    /// </summary>
    public static class PrivateKeyJWTHandler
    {

        public static string ClientAuthJwtCreate(string TokenEndpoint,
            string ClientId,
            string PrivateKeyPFXFile)
        {
            return ClientAuthJwtCreate(TokenEndpoint, ClientId, new X509Certificate2(PrivateKeyPFXFile));
        }

        public static string ClientAuthJwtCreate(string TokenEndpoint,
            string ClientId,
            X509Certificate2 Cert)
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

                // sign with the private key (using RS256 for IdentityServer, or RS512 for NHS Digital)
                signingCredentials: new SigningCredentials(
                  new X509SecurityKey(Cert), "RS512")
            );

            return tokenHandler.WriteToken(securityToken);
        }


        /// <summary>
        /// Wrappers loading the certificate either from a file in the project or from the Configuration extension as a string of the certificate
        /// </summary>
        /// <param name="Config"></param>
        /// <returns></returns>
        public static X509Certificate2 CertifcateForPrivatePublicKeyPair(IConfiguration Config)
        {
            X509Certificate2 xc;

            //Use of EphemeralKeySet is:
            // - sensible to keep it all in memory
            // - important with Azure Web App deploys, otherwise the default permissions don't allow writing of keys to disk

            //Use of MachineKeySet fixes a known issue with Azure Apps resulting in a Bad Data error in FilterPFXStore 

            if (Config["JWT:PubPrivateKeyBase64"] != null)
            {
                //The certificate is loaded into Azure KeyVault. Pulling it down via the 
                //Configuration call, which retrieves the full certificate including private key
                //as a base64 string
                string ppk = Config["JWT:PubPrivateKeyBase64"];
                byte[] ppkBA = Convert.FromBase64String(ppk);
                xc = new X509Certificate2(ppkBA, "", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
            }
            else
            {
                //In some circumstances a PFX File loaded from file could be used
                //It is not recommended to store private keys on a disk, but kept in memory
                xc = new X509Certificate2(Config["JWT:PFXFile"], "", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
            }

            return xc;
        }
    }


    #region Middleware code for publishing a JWKS endpoint

    public class JWKSPublicEndpoint : IMiddleware
    {
        private readonly IConfiguration _configuration;

        public JWKSPublicEndpoint(IConfiguration Config)
        {
            _configuration = Config;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path == "/.well-known/jwks.json")
            {
                context.Response.ContentType = "application/json";

                X509Certificate2 xc = PrivateKeyJWTHandler.CertifcateForPrivatePublicKeyPair(_configuration);

                await context.Response.WriteAsync(JWKSGenerate(xc));
            }
            else
            {
                await next(context);
            }

        }

        public static string JWKSGenerate(string PublicKeyCERFile)
        {
            return JWKSGenerate(new X509Certificate2(PublicKeyCERFile));
        }

        public static string JWKSGenerate(X509Certificate2 Cert)
        {

            JsonWebKeySet jwks = new JsonWebKeySet();

            JsonWebKey jwk = JsonWebKeyConverter.ConvertFromX509SecurityKey(
                new X509SecurityKey(Cert),
                //In this example representAsRsaKey must be true, otherwise NHS Digital doesn't understand the JWK format
                true);

            jwks.Keys.Add(jwk);

            //In this example - the NHS Digital processing of the JWK is case sensitive
            JsonSerializerOptions jso = new JsonSerializerOptions()
            {
                IgnoreNullValues = true, //Tidys up the JWKS output a bit
                PropertyNamingPolicy = new JsonNamingPolicyToLowerCase(),
            };

            string jwksStr = JsonSerializer.Serialize(jwks, jso);
            return jwksStr;
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

    public class JsonNamingPolicyToLowerCase : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToLower();
        }
    }

    #endregion
}
