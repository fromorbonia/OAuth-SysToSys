using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace OAUthSysToSys
{
    public static class PrivateKeyJWTHandler
    {
        public static string JWKSGenerate(string PublicKeyCERFile)
        {
            JsonWebKeySet jwks = new JsonWebKeySet();
            JsonWebKey jwk = JsonWebKeyConverter.ConvertFromX509SecurityKey(
                new X509SecurityKey(new X509Certificate2(PublicKeyCERFile)));
            
            jwks.Keys.Add(jwk);
            return jwks.ToString();
        }

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
    }
}
