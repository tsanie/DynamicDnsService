using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace DynamicDnsService;

public class DDnsService
{
    private readonly HttpClient _httpClient;

    private const string API_BASE = "https://api.cloudflare.com/client/v4";
    private const string ZONE = "example.com";
    private const string DNS_RECORD = "test.example.com";

    public DDnsService(HttpClient client) => _httpClient = client;

    public async Task<ErrorInfo[]?> DDnsAsync()
    {
        string? ip = null;

        foreach (var net in NetworkInterface.GetAllNetworkInterfaces())
        {
            var property = net.GetIPProperties();
            var ipInfo = property.UnicastAddresses.FirstOrDefault(i =>
                i.SuffixOrigin == SuffixOrigin.LinkLayerAddress &&
                i.Address.AddressFamily == AddressFamily.InterNetworkV6 &&
                i.Address.ToString().StartsWith("240e"));
            if (ipInfo != null)
            {
                ip = ipInfo.Address.ToString();
            }
        }
        if (ip == null)
        {
            return new[] { new ErrorInfo(-1, "Cannot get the IPv6 address.") };
        }

        Result? zone = await _httpClient.GetFromJsonAsync<Result>($"{API_BASE}/zones?name={ZONE}");
        if (zone == null || !zone.success)
        {
            return zone?.errors;
        }
        var zoneId = zone.result[0].id;

        Result? record = await _httpClient.GetFromJsonAsync<Result>($"{API_BASE}/zones/{zoneId}/dns_records?name={DNS_RECORD}&type=AAAA");
        if (record == null || !record.success)
        {
            return zone?.errors;
        }
        var recordId = record.result[0].id;
        var ipv6 = record.result[0].content;

        if (ip != ipv6)
        {
            var post = "{\"type\":\"AAAA\",\"name\":\"" + DNS_RECORD + "\",\"content\":\"" + ip + "\",\"ttl\":1,\"proxied\":false}";
            var response = await _httpClient.PutAsync($"{API_BASE}/zones/{zoneId}/dns_records/{recordId}", new StringContent(post, Encoding.UTF8, "application/json"));
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(post, null, response?.StatusCode);
            }
            return new[] { new ErrorInfo(0, $"from {ipv6} to {ip}") };
        }
        return null;
    }
}

#pragma warning disable IDE1006 // Naming Styles

public record ContentInfo(string id, string content);
public record ErrorInfo(int code, string message, ErrorInfo[]? error_chain = null);
public record Result(ContentInfo[] result, bool success, ErrorInfo[] errors);

#pragma warning restore IDE1006 // Naming Styles
