namespace Engineering_IntelligenceTools.Services.Interfaces;
public interface IWebhookSignatureValidator
{
    bool IsValid(string signatureHeader, byte[] rawBody);
}
