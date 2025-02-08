using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net;

class Program
{
    private static readonly string clientId = "YOUR_CLIENT_ID";
    private static readonly string clientSecret = "YOUR_CLIENT_SECRET";
    private static readonly string redirectUri = "http://localhost:5000/callback";
    private static readonly string tokenFile = "tokens.json";
    private static AccessTokenData tokens;

    static async Task Main()
    {
        tokens = LoadTokens();

        if (tokens == null || string.IsNullOrEmpty(tokens.AccessToken))
        {
            Console.WriteLine("Logging in via Discord OAuth...");
            tokens = await LoginAndFetchTokens();
            SaveTokens(tokens);
        }
        else
        {
            Console.WriteLine("🔄 Checking token expiration...");
            if (IsTokenExpired(tokens.ExpiresAt))
            {
                Console.WriteLine("🔄 Refreshing token...");
                tokens = await RefreshAccessToken(tokens.RefreshToken);
                SaveTokens(tokens);
            }
        }

        var user = await GetDiscordUser(tokens.AccessToken);
        var boostedGuilds = await GetUserBoostedGuilds(tokens.AccessToken);

        Console.WriteLine($"\nUser: {user.Username}#{user.Discriminator}");
        Console.WriteLine(user.PremiumType == 0 ? "Nitro: ❌ No Nitro" : "Nitro: ✅ Active Subscription");

        // Display Discord Badges
        string userBadges = GetUserBadges(user.Flags);
        Console.WriteLine($"Badges: {userBadges}");

        Console.WriteLine("\n🔹 Boosted Servers:");
        if (boostedGuilds.Count > 0)
        {
            foreach (var guild in boostedGuilds)
            {
                Console.WriteLine($"✅ {guild.Name} (Boost Level: {guild.PremiumTier})");
            }
        }
        else
        {
            Console.WriteLine("❌ No boosted servers.");
        }
    }

    private static async Task<AccessTokenData> LoginAndFetchTokens()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = $"https://discord.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope=identify%20email%20guilds",
            UseShellExecute = true
        });

        var httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:5000/");
        httpListener.Start();
        var context = await httpListener.GetContextAsync();
        var response = context.Response;
        string responseString = "<html><body><h2>You can close this tab now.</h2></body></html>";
        var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();

        string code = context.Request.QueryString["code"];
        httpListener.Stop();
        return await GetAccessToken(code);
    }

    private static async Task<AccessTokenData> GetAccessToken(string code)
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
            var tokenData = JsonSerializer.Deserialize<AccessTokenData>(responseString);
            tokenData.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);
            return tokenData;
        }
    }

    private static async Task<AccessTokenData> RefreshAccessToken(string refreshToken)
    {
        using (var client = new HttpClient())
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken)
            });

            var response = await client.PostAsync("https://discord.com/api/oauth2/token", content);
            var responseString = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<AccessTokenData>(responseString);
            tokenData.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);
            return tokenData;
        }
    }

    private static string GetUserBadges(int flags)
    {
        var badges = new List<string>();

        if ((flags & 1) != 0) badges.Add("Discord Staff");
        if ((flags & 2) != 0) badges.Add("Discord Partner");
        if ((flags & 4) != 0) badges.Add("HypeSquad Events");
        if ((flags & 8) != 0) badges.Add("Bug Hunter Level 1");
        if ((flags & 64) != 0) badges.Add("HypeSquad Bravery");
        if ((flags & 128) != 0) badges.Add("HypeSquad Brilliance");
        if ((flags & 256) != 0) badges.Add("HypeSquad Balance");
        if ((flags & 512) != 0) badges.Add("Early Supporter");
        if ((flags & 1024) != 0) badges.Add("Team User");
        if ((flags & 4096) != 0) badges.Add("System User");
        if ((flags & 16384) != 0) badges.Add("Bug Hunter Level 2");
        if ((flags & 131072) != 0) badges.Add("Verified Bot Developer");

        return badges.Count > 0 ? string.Join(", ", badges) : "None";
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

    private static async Task<List<Guild>> GetUserBoostedGuilds(string accessToken)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetStringAsync("https://discord.com/api/users/@me/guilds");
            var guilds = JsonSerializer.Deserialize<List<Guild>>(response);

            return guilds.FindAll(guild => guild.PremiumTier > 0);
        }
    }

    private static void SaveTokens(AccessTokenData tokens)
    {
        File.WriteAllText(tokenFile, JsonSerializer.Serialize(tokens));
    }

    private static AccessTokenData LoadTokens()
    {
        if (File.Exists(tokenFile))
        {
            return JsonSerializer.Deserialize<AccessTokenData>(File.ReadAllText(tokenFile));
        }
        return null;
    }

    private static bool IsTokenExpired(DateTime expiresAt)
    {
        return DateTime.UtcNow >= expiresAt;
    }

    private class AccessTokenData
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        public DateTime ExpiresAt { get; set; }
    }

    private class DiscordUser
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("discriminator")]
        public string Discriminator { get; set; }

        [JsonPropertyName("premium_type")]
        public int PremiumType { get; set; } // 0 = No Nitro, 1 = Nitro Classic, 2 = Nitro

        [JsonPropertyName("flags")]
        public int Flags { get; set; } // Bitwise value representing user badges
    }

    private class Guild
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("premium_tier")]
        public int PremiumTier { get; set; } // 0 = No Boosts, 1-3 = Boost Levels
    }
}
