using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.SquareUp;

public class SquareupPaymentSettings : ISettings
{
    public bool UseSandbox { get; set; }
    public string SandboxAccessToken { get; set; }
    public string SandboxApplicationKey { get; set; }
    public string SandboxLocationId { get; set; }
    public string AccessToken { get; set; }
    public string ApplicationKey { get; set; }
    public string LocationId { get; set; }
}
