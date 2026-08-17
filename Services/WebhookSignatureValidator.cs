using System.Security.Cryptography;
using System.Text;
using Engineering_IntelligenceTools.Configuration;
using Engineering_IntelligenceTools.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Engineering_IntelligenceTools.Services;

public class WebhookSignatureValidator : IWebhookSignatureValidator
{
    private const string SignaturePrefix = "sha256=";
    private readonly GitHubOptions _options;

    public WebhookSignatureValidator(IOptions<GitHubOptions> options)
    {
        _options = options.Value;
    }

    public bool IsValid(string signatureHeader, byte[] rawBody)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {   
            return false;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith(SignaturePrefix))
        {
            return false;
        }

        var expectedHex = signatureHeader[SignaturePrefix.Length..];

        var key = Encoding.UTF8.GetBytes(_options.WebhookSecret);
        var computedHash = HMACSHA256.HashData(key, rawBody);
        var computedHex = Convert.ToHexString(computedHash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(expectedHex));
    }
}
