using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.SquareUp.Models;

public record ConfigurationModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    [NopResourceDisplayName("Plugins.Payments.SquareUp.Fields.UseSandbox")]
    public bool UseSandbox { get; set; }
    public bool UseSandbox_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.SquareUp.Fields.SandboxAccessToken")]
    public string SandboxAccessToken { get; set; }
    public bool SandboxAccessToken_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.SquareUp.Fields.SandboxApplicationKey")]
    public string SandboxApplicationKey { get; set; }
    public bool SandboxApplicationKey_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.SquareUp.Fields.SandboxLocationId")]
    public string SandboxLocationId { get; set; }
    public bool SandboxLocationId_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.SquareUp.Fields.AccessToken")]
    public string AccessToken { get; set; }
    public bool AccessToken_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.SquareUp.Fields.ApplicationKey")]
    public string ApplicationKey { get; set; }
    public bool ApplicationKey_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.SquareUp.Fields.LocationId")]
    public string LocationId { get; set; }
    public bool LocationId_OverrideForStore { get; set; }
}
