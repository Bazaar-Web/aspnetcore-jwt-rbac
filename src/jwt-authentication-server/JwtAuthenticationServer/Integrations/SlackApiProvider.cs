using System.Net;
using System.Threading.Tasks;
using Infrastructure.Dotnet.Common;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace JwtAuthenticationServer;

internal class SlackApiProvider : ISlackApiProvider
{
    private const string SlackAuthUrl = "https://slack.com/api/oauth.v2.access";
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly RestClient _restClient = new();

    public SlackApiProvider(IConfiguration configuration)
    {
        _clientId = configuration.GetValue<string>("SlackApp:ClientId");
        _clientSecret = configuration.GetValueWithEnv("SlackApp:ClientSecret", "");
    }

    public async Task<IRestResponse<string>> RequestTokenAsync(string code)
    {
        var request = new RestRequest(SlackAuthUrl, Method.GET);
        request.AddHeader(HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded");
        request.AddQueryParameter("code", code);
        request.AddQueryParameter("client_id", _clientId);
        request.AddQueryParameter("client_secret", _clientSecret);

        return await _restClient.ExecuteAsync<string>(request);
    }
}
