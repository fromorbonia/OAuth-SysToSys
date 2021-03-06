﻿using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace OAUthSysToSys.Pages
{
    public class SysToSysCallModel : PageModel
    {


        //*******************************************************************************
        // Model properties - only used to display values in this example

        public string AuthStatus { get; set; }

        /// <summary>
        /// NEVER display an Access Token in a normal application!
        /// </summary>
        public string AuthAccessToken { get; set; }

        public string AuthEndPoint { get; set; }

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
            try
            {
                //Example based on:  https://www.scottbrady91.com/OAuth/Removing-Shared-Secrets-for-OAuth-Client-Authentication

                var client = new HttpClient();

                //*******************************************************************************
                //Create and configure the Client Credentials endpoint details
                ClientCredentialsTokenRequest cctr = new ClientCredentialsTokenRequest
                {
                    Address = _configuration["OIDC:TokenEndpoint"],
                    GrantType = OidcConstants.GrantTypes.ClientCredentials,
                    //Scopes are not needed for this example 
                    ClientId = _configuration["OIDC:ClientId"],
                };
                this.AuthEndPoint = cctr.Address;


                //*******************************************************************************
                //Retrieve the certificate which contains the private key for signing
                X509Certificate2 xc = PrivateKeyJWTHandler.CertifcateForPrivatePublicKeyPair(_configuration);

                //*******************************************************************************
                //Add a new Client Authentication JWT to the Client Credentials config
                cctr.ClientAssertion = new ClientAssertion
                {
                    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                    Value = PrivateKeyJWTHandler.ClientAuthJwtCreate(cctr.Address,
                            cctr.ClientId,
                            xc)
                };

                //*******************************************************************************
                //Make the client credentials call to get the access token
                TokenResponse response = await client.RequestClientCredentialsTokenAsync(cctr);

                if ((response != null)
                    && (!response.IsError))
                {
                    this.AuthStatus = response.HttpStatusCode.ToString();

                    //************************************************************
                    //NEVER show the Access Token in a real application
                    //  - Getting to grips with OAuth, however, it is useful to see
                    this.AuthAccessToken = response.AccessToken;

                    //************************************************************
                    //Now use the access token to make an arbitrary protected call
                    HttpRequestMessage req = new HttpRequestMessage(System.Net.Http.HttpMethod.Get,
                        _configuration["APIGetUri"]);

                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", response.AccessToken);

                    //************************************************************
                    //The following headers are specific to NHS Digital calls
                    req.Headers.Add("X-Request-ID", (new Guid()).ToString());
                    req.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());
                    req.Headers.Add("NHSD-Session-URID", "555021935107");

                    HttpResponseMessage apiResponse = await new HttpClient().SendAsync(req);

                    this.ResResponse = apiResponse.StatusCode.ToString();
                    this.ResContent = await apiResponse.Content.ReadAsStringAsync();
                }
                else
                {
                    string responseCont = "No content";
                    if ((response != null) && (response.HttpResponse != null))
                    {
                        responseCont = await response.HttpResponse.Content.ReadAsStringAsync();
                    }
                    this.ErrText = string.Format("Failed on Client Credentials Call - {0}, {1}",
                        (response != null ? response.Error : "Could not construct response object"),
                        responseCont);
                }
            }
            catch (Exception ex)
            {
                this.ErrText = string.Format("Exception raised - {0}, {1}, {2}",
                    ex.HResult,
                    ex.Message,
                    ex.StackTrace);

            }
        }

    }
}