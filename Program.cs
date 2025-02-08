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
using System.Linq;

class Program
{
    private static readonly string clientId = "PLACE-CLIENTID-HERE";
    private static readonly string clientSecret = "PLACE-SECRET-HERE";
    private static readonly string redirectUri = "http://localhost:5000/callback";
    private static readonly string tokenFile = "tokens.json";
    private static AccessTokenData tokens;

    static async Task Main()
    {
        tokens = LoadTokens();

        if (tokens == null || string.IsNullOrEmpty(tokens.AccessToken))
        {
            Console.WriteLine("🔑 Logging in via Discord OAuth...");
            tokens = await LoginAndFetchTokens();
            SaveTokens(tokens);
        }
        else
        {
            Console.WriteLine("🔄 Checking token expiration...");
            if (IsTokenExpired(tokens.ExpiresAt))
            {
                Console.WriteLine("🔄 Token expired. Refreshing...");
                tokens = await RefreshAccessToken(tokens.RefreshToken);
                SaveTokens(tokens);
            }
        }

        var user = await GetDiscordUser(tokens.AccessToken);
        var guilds = await GetUserGuilds(tokens.AccessToken);
        var boostedGuilds = await GetUserBoostedGuilds(tokens.AccessToken);
        var connections = await GetUserConnections(tokens.AccessToken);

        Console.WriteLine($"\n👤 User: {user.Username}#{user.Discriminator}");
        Console.WriteLine(user.PremiumType == 0 ? "🚫 Nitro: No Nitro" : "✅ Nitro: Active Subscription");

        string userBadges = GetUserBadges(user.Flags);
        Console.WriteLine($"🏅 Badges: {userBadges}");

        Console.WriteLine("\n🚀 Boosted Servers:");
        foreach (var guild in boostedGuilds)
        {
            Console.WriteLine($"✅ {guild.Name} (Boost Level: {guild.PremiumTier})");
        }

        Console.WriteLine("\n🔗 Connected Accounts:");
        foreach (var connection in connections)
        {
            string visibility = connection.Visibility == 1 ? "Public" : "Private";
            Console.WriteLine($"✅ {connection.Type.ToUpper()} - {connection.Name} (Verified: {connection.Verified}, Visibility: {visibility})");
        }
    }

    // 🔹 Load Tokens from File
    private static AccessTokenData LoadTokens()
    {
        if (!File.Exists(tokenFile)) return null;

        try
        {
            string json = File.ReadAllText(tokenFile);
            return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<AccessTokenData>(json);
        }
        catch
        {
            return null;
        }
    }

    // 🔹 Save Tokens to File
    private static void SaveTokens(AccessTokenData tokens)
    {
        if (tokens == null) return;
        File.WriteAllText(tokenFile, JsonSerializer.Serialize(tokens, new JsonSerializerOptions { WriteIndented = true }));
    }

    // 🔹 Check if Token is Expired
    private static bool IsTokenExpired(DateTime expiresAt) => DateTime.UtcNow >= expiresAt;

    // 🔹 Refresh Access Token
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

    // 🔹 Get Discord User
    private static async Task<DiscordUser> GetDiscordUser(string accessToken)
    {
        string jsonResponse = await SendHttpRequestWithRateLimit("https://discord.com/api/users/@me", accessToken);
        return JsonSerializer.Deserialize<DiscordUser>(jsonResponse);
    }

    // 🔹 Get User's Servers
    private static async Task<List<Guild>> GetUserGuilds(string accessToken)
    {
        string jsonResponse = await SendHttpRequestWithRateLimit("https://discord.com/api/users/@me/guilds", accessToken);
        return JsonSerializer.Deserialize<List<Guild>>(jsonResponse);
    }

    // 🔹 Get User's Boosted Servers
    private static async Task<List<Guild>> GetUserBoostedGuilds(string accessToken)
    {
        var guilds = await GetUserGuilds(accessToken);
        return guilds.FindAll(guild => guild.PremiumTier > 0);
    }

    // 🔹 Get User's Connected Accounts
    private static async Task<List<DiscordConnection>> GetUserConnections(string accessToken)
    {
        string jsonResponse = await SendHttpRequestWithRateLimit("https://discord.com/api/users/@me/connections", accessToken);
        return JsonSerializer.Deserialize<List<DiscordConnection>>(jsonResponse);
    }

    // 🔹 Get User Badges
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

    // 🔹 Send HTTP Request with Rate Limit Handling
    private static async Task<string> SendHttpRequestWithRateLimit(string url, string token, bool isBot = false, int retryCount = 0)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(isBot ? "Bot" : "Bearer", token);
            var response = await client.GetAsync(url);

            if (response.StatusCode == (HttpStatusCode)429) // Rate-limited
            {
                int retryAfter = response.Headers.Contains("Retry-After") ?
                    int.Parse(response.Headers.GetValues("Retry-After").FirstOrDefault()) : 5;

                Console.WriteLine($"⚠️ Rate Limited! Retrying after {retryAfter} seconds...");
                await Task.Delay(retryAfter * 1000);
                return await SendHttpRequestWithRateLimit(url, token, isBot, retryCount + 1);
            }

            return await response.Content.ReadAsStringAsync();
        }
    }

    // 🔹 OAuth2 Login & Token Exchange
    private static async Task<AccessTokenData> LoginAndFetchTokens()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = $"https://discord.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope=identify%20email%20guilds%20connections%20bot",
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

    // 🔹 Exchange Code for Access Token
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
}

// 🎭 Discord Data Models
class AccessTokenData { public string AccessToken { get; set; } public string RefreshToken { get; set; } public int ExpiresIn { get; set; } public DateTime ExpiresAt { get; set; } }
class DiscordUser { public string Username { get; set; } public string Discriminator { get; set; } public int PremiumType { get; set; } public int Flags { get; set; } }
class Guild { public string Id { get; set; } public string Name { get; set; } public int PremiumTier { get; set; } }
class DiscordConnection { public string Id { get; set; } public string Name { get; set; } public string Type { get; set; } public bool Verified { get; set; } public int Visibility { get; set; } }
