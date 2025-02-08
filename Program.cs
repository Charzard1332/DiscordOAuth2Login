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
    private static readonly string clientId = "CLIENTID-HERE";
    private static readonly string clientSecret = "CLIENTSECRET-HERE";
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
            if (tokens == null)
            {
                Console.WriteLine("❌ Failed to obtain access token. Exiting...");
                return;
            }
            SaveTokens(tokens);
        }
        else
        {
            Console.WriteLine("🔄 Checking token expiration...");
            if (IsTokenExpired(tokens.ExpiresAt))
            {
                Console.WriteLine("🔄 Token expired. Refreshing...");
                tokens = await RefreshAccessToken(tokens.RefreshToken);
                if (tokens == null)
                {
                    Console.WriteLine("❌ Failed to refresh access token. Re-login required.");
                    tokens = await LoginAndFetchTokens();
                    if (tokens == null)
                    {
                        Console.WriteLine("❌ Failed to obtain access token. Exiting...");
                        return;
                    }
                }
                SaveTokens(tokens);
            }
        }

        var user = await GetDiscordUser(tokens.AccessToken);
        if (user == null)
        {
            Console.WriteLine("❌ Failed to fetch user data. Exiting...");
            return;
        }

        var guilds = await GetUserGuilds(tokens.AccessToken);
        if (guilds == null)
        {
            Console.WriteLine("❌ Failed to fetch user data. Exiting...");
            return;
        }

        var boostedGuilds = await GetUserBoostedGuilds(tokens.AccessToken);
        if (boostedGuilds == null)
        {
            Console.WriteLine("❌ Failed to fetch user data. Exiting...");
            return;
        }

        var connections = await GetUserConnections(tokens.AccessToken);
        if (connections == null)
        {
            Console.WriteLine("❌ Failed to fetch user data. Exiting...");
            return;
        }

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

            Console.WriteLine($"📩 OAuth2 Token Response: {responseString}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error: Failed to retrieve access token. Status: {response.StatusCode}");
                return null;
            }

            try
            {
                var tokenData = JsonSerializer.Deserialize<AccessTokenData>(responseString);

                if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
                {
                    Console.WriteLine("❌ Error: `access_token` is missing from the response!");
                    return null;
                }

                tokenData.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);
                return tokenData;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"❌ JSON Parsing Error: {ex.Message}");
                return null;
            }
        }
    }

    private static async Task<AccessTokenData> LoginAndFetchTokens()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = $"https://discord.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope=identify%20email%20guilds%20connections",
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

    private static AccessTokenData LoadTokens()
    {
        if (!File.Exists(tokenFile)) return null;
        string json = File.ReadAllText(tokenFile);
        return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<AccessTokenData>(json);
    }

    private static void SaveTokens(AccessTokenData tokens)
    {
        if (tokens == null) return;
        File.WriteAllText(tokenFile, JsonSerializer.Serialize(tokens, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static bool IsTokenExpired(DateTime expiresAt) => DateTime.UtcNow >= expiresAt;

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

            Console.WriteLine($"📩 Token Refresh Response: {responseString}");

            var tokenData = JsonSerializer.Deserialize<AccessTokenData>(responseString);
            if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
            {
                Console.WriteLine("❌ Failed to refresh token.");
                return null;
            }

            tokenData.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);
            return tokenData;
        }
    }

    private static async Task<DiscordUser> GetDiscordUser(string accessToken)
    {
        string jsonResponse = await SendHttpRequestWithRateLimit("https://discord.com/api/users/@me", accessToken);

        Console.WriteLine($"API Response (User): {jsonResponse}");

        if (string.IsNullOrEmpty(jsonResponse))
        {
            Console.WriteLine("❌ Failed to fetch user data. API response was empty.");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<DiscordUser>(jsonResponse);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"❌ JSON Parsing Error: {ex.Message}");
            return null;
        }
    }


    private static async Task<List<Guild>> GetUserGuilds(string accessToken)
    {
        string jsonResponse = await SendHttpRequestWithRateLimit("https://discord.com/api/users/@me/guilds", accessToken);

        Console.WriteLine($"📩 API Response (Guilds): {jsonResponse}");

        if (string.IsNullOrWhiteSpace(jsonResponse) || jsonResponse.StartsWith("{"))
        {
            Console.WriteLine("❌ Error: API returned an invalid response for guilds.");
            return new List<Guild>();
        }

        return JsonSerializer.Deserialize<List<Guild>>(jsonResponse);
    }

    private static async Task<List<Guild>> GetUserBoostedGuilds(string accessToken)
    {
        var guilds = await GetUserGuilds(accessToken);
        Console.WriteLine($"API Response: (Boosted Servers): {guilds}");
        return guilds?.FindAll(guild => guild.PremiumTier > 0) ?? new List<Guild>();
    }

    private static async Task<List<DiscordConnection>> GetUserConnections(string accessToken)
    {
        string jsonResponse = await SendHttpRequestWithRateLimit("https://discord.com/api/users/@me/connections", accessToken);

        // Log the raw response for debugging
        Console.WriteLine($"📩 API Response (Connections): {jsonResponse}");

        // If the response is empty or not an array, return an empty list
        if (string.IsNullOrWhiteSpace(jsonResponse) || jsonResponse.StartsWith("{"))
        {
            Console.WriteLine("❌ Error: API returned an invalid response for connections.");
            return new List<DiscordConnection>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<DiscordConnection>>(jsonResponse);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"❌ JSON Deserialization Error: {ex.Message}");
            return new List<DiscordConnection>();
        }
    }

    private static async Task<string> SendHttpRequestWithRateLimit(string url, string token, bool isBot = false, int retryCount = 0)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync(url);
            string responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == (HttpStatusCode)429)
            {
                int retryAfter = response.Headers.Contains("Retry-After") ?
                    int.Parse(response.Headers.GetValues("Retry-After").FirstOrDefault()) : 5;

                Console.WriteLine($"⚠️ Rate Limited! Retrying after {retryAfter} seconds...");
                await Task.Delay(retryAfter * 1000);
                return await SendHttpRequestWithRateLimit(url, token, isBot, retryCount + 1);
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ API Error ({response.StatusCode}): {responseString}");
                return string.Empty;
            }

            return responseString;
        }
    }
}

// 🎭 Discord Data Models
class AccessTokenData
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    public DateTime ExpiresAt { get; set; }
}
class DiscordUser { public string Username { get; set; } public string Discriminator { get; set; } public int PremiumType { get; set; } public int Flags { get; set; } }
class Guild { public string Id { get; set; } public string Name { get; set; } public int PremiumTier { get; set; } }
class DiscordConnection { public string Id { get; set; } public string Name { get; set; } public string Type { get; set; } public bool Verified { get; set; } public int Visibility { get; set; } }
