# Example: Web Server App Authenticating with NHS Digital OAuth

This example shows system to system authentication (OAuth Client Credentials flow) using Private Key JWT OAuth Client Authentication model, and how to publish your own JWKS endpoint with simple custom middleware.

## Getting Started

See - full example workthrough here: [Medium - Example Web Server App Authenticating with NHS Digital OAuth](https://aubyncrawford.medium.com/example-web-server-app-authenticating-with-nhs-digital-oauth-57f58f3fb62f)

### Prerequisites

This project was created with Visual Studio 2019, and is an ASP.NET Core 3 Web Application

The project is also demonstrating publishing a JWKS endpoint, and I chose to deploy this as an Azure Web App using Azure Key Vault to store the private / public key pair


## Deployment

Requires an Azure subscription if using the Azure Web Publish and Azure Key Vault

## Built With

* Visual Studio 2019 - ASP.NET Core 3 and Razor pages
* NuGet package:
    * System.IdentityModel.Tokens.Jwt - Microsoft standard JWT libraries
    * IdentityModel - helper library for OAuth and OIDC
* NuGet packages for using Azure Key Vault:
    *  Azure.Identity
    *  Azure.Extensions.AspNetCore.Configuration.Secrets

## License

This is just code brought together to help others get started quickly, nothing unique. Licensed under Zero-Clause BSD - see [LICENSE.md](LICENSE.md)

## Acknowledgments

* Main code segments, using the IdentityModel, were based on the code in Scott Brady’s blog: https://www.scottbrady91.com/OAuth/Removing-Shared-Secrets-for-OAuth-Client-Authentication
* The middleware for publishing the JWKS endpoint was just applying a bit of logic and a number of baseline Microsoft documents
* Billie Thompson - README template - [PurpleBooth](https://github.com/PurpleBooth)

