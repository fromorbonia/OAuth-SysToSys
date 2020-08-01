using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OAUthSysToSys.Pages
{
    //[Authorize]
    public class SysToSysCallModel : PageModel
    {
        public string AuthStatus { get; set; }

        public string AuthAccessToken { get; set; }

        public string ResResponse { get; set; }

        public string ResContent { get; set; }

        public string ErrText { get; set; }



        private readonly IConfiguration _configuration;

        public SysToSysCallModel(IConfiguration Config)
        {
            _configuration = Config;
        }
        public async Task OnGet()
        {
            //Example based on:  https://www.scottbrady91.com/OAuth/Removing-Shared-Secrets-for-OAuth-Client-Authentication

            var client = new HttpClient();
            ClientCredentialsTokenRequest cctr = new ClientCredentialsTokenRequest
            {
                Address = _configuration["OIDCLocal:TokenEndpoint"],
                GrantType = OidcConstants.GrantTypes.ClientCredentials,
                Scope = "api1",
                ClientId = _configuration["OIDCLocal:ClientId"],
            };

            //Create and configure the Authentication JWT
            cctr.ClientAssertion = new ClientAssertion
            {
                Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                Value = PrivateKeyJWTHandler.ClientAuthJwtCreate(cctr.Address,
                        cctr.ClientId,
                        _configuration["JWT:PrivateKeyPFXFile"])
            };

            TokenResponse response = await client.RequestClientCredentialsTokenAsync(cctr);

            this.AuthStatus = response.HttpStatusCode.ToString();
            this.AuthAccessToken = response.AccessToken;

            //************************************************************
            //Now use the access token to make an arbitrary protected call

            HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Get,
                "https://localhost:5001/diagnostics");

            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", response.AccessToken);

            HttpResponseMessage oauthResponse = await new HttpClient().SendAsync(req);

            this.ResResponse = oauthResponse.StatusCode.ToString();
            this.ResContent = await oauthResponse.Content.ReadAsStringAsync();
        }

    }
}