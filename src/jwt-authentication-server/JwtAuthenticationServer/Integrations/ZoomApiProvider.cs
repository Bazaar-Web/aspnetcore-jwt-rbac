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
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace JwtAuthenticationServer.Integrations
{
    // Zoom Integration - External Video Conferencing Service
    internal class ZoomApiProvider : IZoomApiProvider
    {
        private const string ZoomApiUrl = "https://api.zoom.us/v2";
        private const string ZoomOAuthUrl = "https://zoom.us/oauth/token";
        private const string ZoomAuthUrl = "https://zoom.us/oauth/authorize";
        
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _accountId;
        private readonly RestClient _restClient = new();

        public ZoomApiProvider(IConfiguration configuration)
        {
            _clientId = configuration.GetValue<string>("Zoom:ClientId");
            _clientSecret = configuration.GetValueWithEnv("Zoom:ClientSecret", "");
            _apiKey = configuration.GetValue<string>("Zoom:ApiKey");
            _apiSecret = configuration.GetValueWithEnv("Zoom:ApiSecret", "");
            _accountId = configuration.GetValue<string>("Zoom:AccountId");
        }

        private string GenerateJwtToken()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_apiSecret);
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("iss", _apiKey),
                    new System.Security.Claims.Claim("exp", ((int)(DateTime.UtcNow.AddMinutes(30) - new DateTime(1970, 1, 1)).TotalSeconds).ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<IRestResponse<string>> RequestTokenAsync(string code, string redirectUri)
        {
            var request = new RestRequest(ZoomOAuthUrl, Method.POST);
            request.AddHeader("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"))}");
            request.AddHeader(HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded");
            
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("code", code);
            request.AddParameter("redirect_uri", redirectUri);

            return await _restClient.ExecuteAsync<string>(request);
        }

        public async Task<string> RefreshTokenAsync(string refreshToken)
        {
            var request = new RestRequest(ZoomOAuthUrl, Method.POST);
            request.AddHeader("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"))}");
            request.AddHeader(HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded");
            
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", refreshToken);

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetUserInfoAsync(string accessToken = null)
        {
            var request = new RestRequest($"{ZoomApiUrl}/users/me", Method.GET);
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.AddHeader("Authorization", $"Bearer {accessToken}");
            }
            else
            {
                request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");
            }

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> CreateMeetingAsync(string topic, DateTime startTime, int duration, string password = null, bool waitingRoom = true)
        {
            var request = new RestRequest($"{ZoomApiUrl}/users/me/meetings", Method.POST);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");
            request.AddHeader("Content-Type", "application/json");

            var meetingData = new
            {
                topic = topic,
                type = 2, // Scheduled meeting
                start_time = startTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                duration = duration,
                timezone = "UTC",
                password = password,
                settings = new
                {
                    host_video = true,
                    participant_video = true,
                    join_before_host = false,
                    mute_upon_entry = true,
                    watermark = false,
                    use_pmi = false,
                    approval_type = 2,
                    audio = "both",
                    auto_recording = "none",
                    waiting_room = waitingRoom
                }
            };

            request.AddJsonBody(meetingData);

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> CreateInstantMeetingAsync(string topic, string password = null)
        {
            var request = new RestRequest($"{ZoomApiUrl}/users/me/meetings", Method.POST);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");
            request.AddHeader("Content-Type", "application/json");

            var meetingData = new
            {
                topic = topic,
                type = 1, // Instant meeting
                password = password,
                settings = new
                {
                    host_video = true,
                    participant_video = true,
                    join_before_host = false,
                    mute_upon_entry = true,
                    waiting_room = true
                }
            };

            request.AddJsonBody(meetingData);

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetMeetingAsync(string meetingId)
        {
            var request = new RestRequest($"{ZoomApiUrl}/meetings/{meetingId}", Method.GET);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> UpdateMeetingAsync(string meetingId, string topic = null, DateTime? startTime = null, int? duration = null)
        {
            var request = new RestRequest($"{ZoomApiUrl}/meetings/{meetingId}", Method.PATCH);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");
            request.AddHeader("Content-Type", "application/json");

            var updateData = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(topic))
                updateData["topic"] = topic;
            
            if (startTime.HasValue)
                updateData["start_time"] = startTime.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
            
            if (duration.HasValue)
                updateData["duration"] = duration.Value;

            request.AddJsonBody(updateData);

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<bool> DeleteMeetingAsync(string meetingId)
        {
            var request = new RestRequest($"{ZoomApiUrl}/meetings/{meetingId}", Method.DELETE);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");

            var response = await _restClient.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public async Task<string> ListMeetingsAsync(string userId = "me", int pageSize = 30)
        {
            var request = new RestRequest($"{ZoomApiUrl}/users/{userId}/meetings", Method.GET);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");
            request.AddQueryParameter("page_size", pageSize.ToString());

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetMeetingParticipantsAsync(string meetingId)
        {
            var request = new RestRequest($"{ZoomApiUrl}/meetings/{meetingId}/participants", Method.GET);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> CreateWebinarAsync(string topic, DateTime startTime, int duration, string password = null)
        {
            var request = new RestRequest($"{ZoomApiUrl}/users/me/webinars", Method.POST);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");
            request.AddHeader("Content-Type", "application/json");

            var webinarData = new
            {
                topic = topic,
                type = 5, // Webinar
                start_time = startTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                duration = duration,
                timezone = "UTC",
                password = password,
                settings = new
                {
                    host_video = true,
                    panelists_video = true,
                    practice_session = false,
                    hd_video = false,
                    approval_type = 2,
                    audio = "both",
                    auto_recording = "none",
                    registrants_email_notification = true
                }
            };

            request.AddJsonBody(webinarData);

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> AddMeetingRegistrantAsync(string meetingId, string email, string firstName, string lastName)
        {
            var request = new RestRequest($"{ZoomApiUrl}/meetings/{meetingId}/registrants", Method.POST);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");
            request.AddHeader("Content-Type", "application/json");

            var registrantData = new
            {
                email = email,
                first_name = firstName,
                last_name = lastName
            };

            request.AddJsonBody(registrantData);

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetMeetingRegistrantsAsync(string meetingId)
        {
            var request = new RestRequest($"{ZoomApiUrl}/meetings/{meetingId}/registrants", Method.GET);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetMeetingRecordingsAsync(string meetingId)
        {
            var request = new RestRequest($"{ZoomApiUrl}/meetings/{meetingId}/recordings", Method.GET);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<bool> DeleteMeetingRecordingAsync(string meetingId, string recordingId)
        {
            var request = new RestRequest($"{ZoomApiUrl}/meetings/{meetingId}/recordings/{recordingId}", Method.DELETE);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");

            var response = await _restClient.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public async Task<string> CreateRoomAsync(string name, int capacity, string roomType = "Conference Room")
        {
            var request = new RestRequest($"{ZoomApiUrl}/rooms", Method.POST);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");
            request.AddHeader("Content-Type", "application/json");

            var roomData = new
            {
                name = name,
                type = roomType,
                capacity = capacity
            };

            request.AddJsonBody(roomData);

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetAccountSettingsAsync()
        {
            var request = new RestRequest($"{ZoomApiUrl}/accounts/{_accountId}/settings", Method.GET);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetDashboardMeetingsAsync(DateTime from, DateTime to)
        {
            var request = new RestRequest($"{ZoomApiUrl}/metrics/meetings", Method.GET);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");
            request.AddQueryParameter("from", from.ToString("yyyy-MM-dd"));
            request.AddQueryParameter("to", to.ToString("yyyy-MM-dd"));

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<bool> SendChatMessageAsync(string toJid, string message)
        {
            var request = new RestRequest($"{ZoomApiUrl}/im/chat/messages", Method.POST);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");
            request.AddHeader("Content-Type", "application/json");

            var messageData = new
            {
                message = message,
                to_jid = toJid
            };

            request.AddJsonBody(messageData);

            var response = await _restClient.ExecuteAsync(request);
            return response.IsSuccessful;
        }

        public async Task<string> GetUserSettingsAsync(string userId = "me")
        {
            var request = new RestRequest($"{ZoomApiUrl}/users/{userId}/settings", Method.GET);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> UpdateUserSettingsAsync(string userId, object settings)
        {
            var request = new RestRequest($"{ZoomApiUrl}/users/{userId}/settings", Method.PATCH);
            request.AddHeader("Authorization", $"Bearer {GenerateJwtToken()}");
            request.AddHeader("Content-Type", "application/json");

            request.AddJsonBody(settings);

            var response = await _restClient.ExecuteAsync(request);
            return response.Content;
        }
    }

    // Interface for dependency injection
    public interface IZoomApiProvider
    {
        Task<IRestResponse<string>> RequestTokenAsync(string code, string redirectUri);
        Task<string> RefreshTokenAsync(string refreshToken);
        Task<string> GetUserInfoAsync(string accessToken = null);
        Task<string> CreateMeetingAsync(string topic, DateTime startTime, int duration, string password = null, bool waitingRoom = true);
        Task<string> CreateInstantMeetingAsync(string topic, string password = null);
        Task<string> GetMeetingAsync(string meetingId);
        Task<string> UpdateMeetingAsync(string meetingId, string topic = null, DateTime? startTime = null, int? duration = null);
        Task<bool> DeleteMeetingAsync(string meetingId);
        Task<string> ListMeetingsAsync(string userId = "me", int pageSize = 30);
        Task<string> GetMeetingParticipantsAsync(string meetingId);
        Task<string> CreateWebinarAsync(string topic, DateTime startTime, int duration, string password = null);
        Task<string> AddMeetingRegistrantAsync(string meetingId, string email, string firstName, string lastName);
        Task<string> GetMeetingRegistrantsAsync(string meetingId);
        Task<string> GetMeetingRecordingsAsync(string meetingId);
        Task<bool> DeleteMeetingRecordingAsync(string meetingId, string recordingId);
        Task<string> CreateRoomAsync(string name, int capacity, string roomType = "Conference Room");
        Task<string> GetAccountSettingsAsync();
        Task<string> GetDashboardMeetingsAsync(DateTime from, DateTime to);
        Task<bool> SendChatMessageAsync(string toJid, string message);
        Task<string> GetUserSettingsAsync(string userId = "me");
        Task<string> UpdateUserSettingsAsync(string userId, object settings);
    }
}