using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Newtonsoft.Json;
using JwtAuthenticationServer.Models;

namespace JwtAuthenticationServer.Integrations
{
    // Discord Integration - External Gaming/Communication Service
    internal class DiscordApiProvider : IDiscordApiProvider
    {
        private const string DiscordApiUrl = "https://discord.com/api/v10";
        private const string DiscordOAuthUrl = "https://discord.com/api/oauth2/token";
        private const string DiscordWebhookUrl = "https://discord.com/api/webhooks/{0}/{1}";
        
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _botToken;
        private readonly string _webhookId;
        private readonly string _webhookToken;
        private readonly RestClient _restClient = new();

        public DiscordApiProvider(IConfiguration configuration)
        {
            _clientId = configuration.GetValue<string>("Discord:ClientId");
            _clientSecret = configuration.GetValueWithEnv("Discord:ClientSecret", "");
            _botToken = configuration.GetValueWithEnv("DISCORD_BOT_TOKEN", "");
            _webhookId = configuration.GetValue<string>("Discord:WebhookId");
            _webhookToken = configuration.GetValueWithEnv("Discord:WebhookToken", "");
        }

        public async Task<IRestResponse<string>> RequestTokenAsync(string code, string redirectUri)
        {
            var request = new RestRequest(DiscordOAuthUrl, Method.POST);
            request.AddHeader(HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded");
            
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("client_id", _clientId),
                new("client_secret", _clientSecret),
                new("grant_type", "authorization_code"),
                new("code", code),
                new("redirect_uri", redirectUri)
            };

            var content = new FormUrlEncodedContent(parameters);
            request.AddParameter("application/x-www-form-urlencoded", await content.ReadAsStringAsync(), ParameterType.RequestBody);

            return await _restClient.ExecuteAsync<string>(request);
        }

        public async Task<string> GetUserInfoAsync(string accessToken)
        {
            var request = new RestRequest($"{DiscordApiUrl}/users/@me", Method.GET);
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetUserGuildsAsync(string accessToken)
        {
            var request = new RestRequest($"{DiscordApiUrl}/users/@me/guilds", Method.GET);
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<bool> SendDirectMessageAsync(string userId, string content)
        {
            try
            {
                // First, create a DM channel
                var createDmRequest = new RestRequest($"{DiscordApiUrl}/users/@me/channels", Method.POST);
                createDmRequest.AddHeader("Authorization", $"Bot {_botToken}");
                createDmRequest.AddHeader("Content-Type", "application/json");
                createDmRequest.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");

                var dmPayload = new { recipient_id = userId };
                createDmRequest.AddJsonBody(dmPayload);

                var dmResponse = await _restClient.ExecuteAsync(createDmRequest);
                if (!dmResponse.IsSuccessful) return false;

                var dmChannel = JsonConvert.DeserializeObject<dynamic>(dmResponse.Content);
                string channelId = dmChannel.id;

                // Send message to the DM channel
                var messageRequest = new RestRequest($"{DiscordApiUrl}/channels/{channelId}/messages", Method.POST);
                messageRequest.AddHeader("Authorization", $"Bot {_botToken}");
                messageRequest.AddHeader("Content-Type", "application/json");
                messageRequest.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");

                var messagePayload = new { content = content };
                messageRequest.AddJsonBody(messagePayload);

                var messageResponse = await _restClient.ExecuteAsync(messageRequest);
                return messageResponse.IsSuccessful;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendChannelMessageAsync(string channelId, string content)
        {
            var request = new RestRequest($"{DiscordApiUrl}/channels/{channelId}/messages", Method.POST);
            request.AddHeader("Authorization", $"Bot {_botToken}");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");

            var payload = new { content = content };
            request.AddJsonBody(payload);

            var response = await _restClient.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public async Task<bool> SendEmbedMessageAsync(string channelId, string title, string description, string color = "0x00ff00")
        {
            var request = new RestRequest($"{DiscordApiUrl}/channels/{channelId}/messages", Method.POST);
            request.AddHeader("Authorization", $"Bot {_botToken}");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");

            var embed = new
            {
                title = title,
                description = description,
                color = Convert.ToInt32(color, 16),
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            var payload = new { embeds = new[] { embed } };
            request.AddJsonBody(payload);

            var response = await _restClient.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public async Task<bool> SendWebhookMessageAsync(string content, string username = null, string avatarUrl = null)
        {
            if (string.IsNullOrEmpty(_webhookId) || string.IsNullOrEmpty(_webhookToken))
                return false;

            var webhookUrl = string.Format(DiscordWebhookUrl, _webhookId, _webhookToken);
            var request = new RestRequest(webhookUrl, Method.POST);
            request.AddHeader("Content-Type", "application/json");

            var payload = new
            {
                content = content,
                username = username,
                avatar_url = avatarUrl
            };

            request.AddJsonBody(payload);

            var response = await _restClient.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public async Task<string> GetGuildMembersAsync(string guildId, int limit = 100)
        {
            var request = new RestRequest($"{DiscordApiUrl}/guilds/{guildId}/members", Method.GET);
            request.AddHeader("Authorization", $"Bot {_botToken}");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");
            request.AddQueryParameter("limit", limit.ToString());

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<bool> AddRoleToUserAsync(string guildId, string userId, string roleId)
        {
            var request = new RestRequest($"{DiscordApiUrl}/guilds/{guildId}/members/{userId}/roles/{roleId}", Method.PUT);
            request.AddHeader("Authorization", $"Bot {_botToken}");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");
            request.AddHeader("Content-Length", "0");

            var response = await _restClient.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public async Task<bool> RemoveRoleFromUserAsync(string guildId, string userId, string roleId)
        {
            var request = new RestRequest($"{DiscordApiUrl}/guilds/{guildId}/members/{userId}/roles/{roleId}", Method.DELETE);
            request.AddHeader("Authorization", $"Bot {_botToken}");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");

            var response = await _restClient.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public async Task<string> CreateInviteAsync(string channelId, int maxAge = 86400, int maxUses = 0)
        {
            var request = new RestRequest($"{DiscordApiUrl}/channels/{channelId}/invites", Method.POST);
            request.AddHeader("Authorization", $"Bot {_botToken}");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");

            var payload = new
            {
                max_age = maxAge,
                max_uses = maxUses,
                temporary = false,
                unique = true
            };

            request.AddJsonBody(payload);

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<bool> KickMemberAsync(string guildId, string userId, string reason = null)
        {
            var request = new RestRequest($"{DiscordApiUrl}/guilds/{guildId}/members/{userId}", Method.DELETE);
            request.AddHeader("Authorization", $"Bot {_botToken}");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");

            if (!string.IsNullOrEmpty(reason))
            {
                request.AddHeader("X-Audit-Log-Reason", reason);
            }

            var response = await _restClient.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public async Task<bool> BanMemberAsync(string guildId, string userId, string reason = null, int deleteMessageDays = 0)
        {
            var request = new RestRequest($"{DiscordApiUrl}/guilds/{guildId}/bans/{userId}", Method.PUT);
            request.AddHeader("Authorization", $"Bot {_botToken}");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");

            if (!string.IsNullOrEmpty(reason))
            {
                request.AddHeader("X-Audit-Log-Reason", reason);
            }

            var payload = new { delete_message_days = deleteMessageDays };
            request.AddJsonBody(payload);

            var response = await _restClient.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public async Task<string> GetChannelMessagesAsync(string channelId, int limit = 50)
        {
            var request = new RestRequest($"{DiscordApiUrl}/channels/{channelId}/messages", Method.GET);
            request.AddHeader("Authorization", $"Bot {_botToken}");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");
            request.AddQueryParameter("limit", limit.ToString());

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<bool> DeleteMessageAsync(string channelId, string messageId)
        {
            var request = new RestRequest($"{DiscordApiUrl}/channels/{channelId}/messages/{messageId}", Method.DELETE);
            request.AddHeader("Authorization", $"Bot {_botToken}");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");

            var response = await _restClient.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public async Task<bool> AddReactionAsync(string channelId, string messageId, string emoji)
        {
            var request = new RestRequest($"{DiscordApiUrl}/channels/{channelId}/messages/{messageId}/reactions/{emoji}/@me", Method.PUT);
            request.AddHeader("Authorization", $"Bot {_botToken}");
            request.AddHeader("User-Agent", "DiscordBot (https://example.com, 1.0)");
            request.AddHeader("Content-Length", "0");

            var response = await _restClient.ExecuteAsync(request);
            return response.IsSuccessful;
        }
    }

    // Interface for dependency injection
    public interface IDiscordApiProvider
    {
        Task<IRestResponse<string>> RequestTokenAsync(string code, string redirectUri);
        Task<string> GetUserInfoAsync(string accessToken);
        Task<string> GetUserGuildsAsync(string accessToken);
        Task<bool> SendDirectMessageAsync(string userId, string content);
        Task<bool> SendChannelMessageAsync(string channelId, string content);
        Task<bool> SendEmbedMessageAsync(string channelId, string title, string description, string color = "0x00ff00");
        Task<bool> SendWebhookMessageAsync(string content, string username = null, string avatarUrl = null);
        Task<string> GetGuildMembersAsync(string guildId, int limit = 100);
        Task<bool> AddRoleToUserAsync(string guildId, string userId, string roleId);
        Task<bool> RemoveRoleFromUserAsync(string guildId, string userId, string roleId);
        Task<string> CreateInviteAsync(string channelId, int maxAge = 86400, int maxUses = 0);
        Task<bool> KickMemberAsync(string guildId, string userId, string reason = null);
        Task<bool> BanMemberAsync(string guildId, string userId, string reason = null, int deleteMessageDays = 0);
        Task<string> GetChannelMessagesAsync(string channelId, int limit = 50);
        Task<bool> DeleteMessageAsync(string channelId, string messageId);
        Task<bool> AddReactionAsync(string channelId, string messageId, string emoji);
    }
}