using System;
using System.Net;
using System.Threading.Tasks;
using Infrastructure.Dotnet.Common;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace JwtAuthenticationServer;

internal class MicrosoftGraphApiProvider : IMicrosoftGraphApiProvider
{
    private const string MicrosoftAuthUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    private readonly IConfiguration _configuration;
    private readonly RestClient _restClient = new();

    public MicrosoftGraphApiProvider(IConfiguration configuration)
        => _configuration = configuration;

    public async Task<IRestResponse<string>> RequestTokenAsync(Provider provider, string code)
    {
        var section = provider switch
        {
            Provider.Teams => "TeamsApp",
            Provider.AzureActiveDirectory => "AzureActiveDirectoryApp",
            Provider.AzureApiManagement => "AzureApiManagementApp",
            _ => throw new NotImplementedException($"Provider {provider} not supported by Microsoft Graph API")
        };

        var request = new RestRequest(MicrosoftAuthUrl, Method.POST);
        request.AddHeader(HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded");
        request.AddParameter("grant_type", "authorization_code");
        request.AddParameter("code", code);
        request.AddParameter("client_id", _configuration.GetValue<string>($"{section}:ClientId"));
        request.AddParameter("scope", _configuration.GetValue<string>($"{section}:RequestedPermissions"));
        request.AddParameter("redirect_uri", _configuration.GetValue<string>($"{section}:CallbackUrl"));
        request.AddParameter("client_secret", _configuration.GetValueWithEnv($"{section}:ClientSecret", ""));

        return await _restClient.ExecuteAsync<string>(request);
    }
}
