using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DiscordLoginApp
{
    internal class Program
    {
        private static readonly string clientId = "1337672783450341417";
        private static readonly string clientSecret = "GIURfdw-xkxD2Aztm7YpvHav4afwHEiZ";
        private static readonly string redirectUri = "http://localhost:5000/callback";
        private static readonly string oauthUrl = $"https://discord.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope=identify%20email%20guilds";

        static async Task Main()
        {
            Console.WriteLine("Opening Discord Login Page...");
            Process.Start(new ProcessStartInfo { FileName = oauthUrl, UseShellExecute = true });

            var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:5000/");
            httpListener.Start();

            Console.WriteLine("Waiting for authentication...");
            var context = await httpListener.GetContextAsync();
            var response = context.Response;
            string responseString = "<html><body><h2>You can close this tab now.</h2></body></html>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

            string code = context.Request.QueryString["code"];
            if (string.IsNullOrEmpty(code))
            {
                Console.WriteLine("Authorization failed.");
                return;
            }

            Console.WriteLine("Authorization successful. Fetching user details...");
            var token = await GetAccessToken(code);
            var user = await GetDiscordUser(token);

            Console.WriteLine($"\nUser: {user.Username}#{user.Discriminator}");
            Console.WriteLine(user.PremiumType == 0 ? "Nitro: No Nitro" : "Nitro: Active Subscription");

            httpListener.Stop();
        }

        private static async Task<string> GetAccessToken(string code)
        {
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUri)
        });

                var response = await client.PostAsync("https://discord.com/api/oauth2/token", content);
                var responseString = await response.Content.ReadAsStringAsync();

                using (var jsonDoc = JsonDocument.Parse(responseString))
                {
                    return jsonDoc.RootElement.GetProperty("access_token").GetString();
                }
            }
        }

        private static async Task<DiscordUser> GetDiscordUser(string accessToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.GetStringAsync("https://discord.com/api/users/@me");
                return JsonSerializer.Deserialize<DiscordUser>(response);
            }
        }

        private class DiscordUser
        {
            [JsonPropertyName("username")]
            public string Username { get; set; }

            [JsonPropertyName("discriminator")]
            public string Discriminator { get; set; }

            [JsonPropertyName("premium_type")]
            public int PremiumType { get; set; } // 0 = No Nitro, 1 = Nitro Classic, 2 = Nitro
        }
    }
}
