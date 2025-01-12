using Nop.Core;

namespace Nop.Plugin.Payments.SquareUp;

/// <summary>
/// Represents plugin constants
/// </summary>
public class SquareupPaymentDefaults
{
    /// <summary>
    /// Name of the view component to display plugin in public store
    /// </summary>
    public const string VIEW_COMPONENT_NAME = "PaymentSquareUpViewComponent";

    /// <summary>
    /// Payment status "APPROVED"
    /// </summary>
    public const string PAYMENT_APPROVED_STATUS = "APPROVED";

    /// <summary>
    /// Payment status "COMPLETED"
    /// </summary>
    public const string PAYMENT_COMPLETED_STATUS = "COMPLETED";

    /// <summary>
    /// Payment status "FAILED"
    /// </summary>
    public const string PAYMENT_FAILED_STATUS = "FAILED";

    /// <summary>
    /// Payment status "CANCELED"
    /// </summary>
    public const string PAYMENT_CANCELED_STATUS = "CANCELED";

    /// <summary>
    /// Location status "ACTIVE"
    /// </summary>
    public const string LOCATION_STATUS_ACTIVE = "ACTIVE";

    /// <summary>
    /// Location capability "CREDIT_CARD_PROCESSING"
    /// </summary>
    public const string LOCATION_CAPABILITIES_PROCESSING = "CREDIT_CARD_PROCESSING";

    /// <summary>
    /// Refund status "PENDING"
    /// </summary>
    public const string REFUND_STATUS_PENDING = "PENDING";

    /// <summary>
    /// Refund status "COMPLETED"
    /// </summary>
    public const string REFUND_STATUS_COMPLETED = "COMPLETED";

    /// <summary>
    /// Square payment method system name
    /// </summary>
    public static string SystemName => "Payments.SquareUp";

    /// <summary>
    /// User agent used to request Square services
    /// </summary>
    public static string UserAgent => $"nopCommerce-{NopVersion.CURRENT_VERSION}";

    /// <summary>
    /// One page checkout route name
    /// </summary>
    public static string OnePageCheckoutRouteName => "CheckoutOnePage";

    /// <summary>
    /// Path to the Square payment form js script
    /// </summary>
    public static string PaymentFormScriptPath => "https://web.squarecdn.com/v1/square.js";

    /// <summary>
    /// Path to the Square payment form js script
    /// </summary>
    public static string SandboxPaymentFormScriptPath => "https://sandbox.web.squarecdn.com/v1/square.js";

    //
    // The items below this line are not currently used, but leaving here in case they are needed for 
    // future versions
    //

    ///// <summary>
    ///// Name of the route to the access token callback
    ///// </summary>
    //public static string AccessTokenRoute => "Plugin.Payments.Square.AccessToken";

    ///// <summary>
    ///// Name of the renew access token schedule task
    ///// </summary>
    //public static string RenewAccessTokenTaskName => "Renew access token (Square payment)";

    ///// <summary>
    ///// Type of the renew access token schedule task
    ///// </summary>
    //public static string RenewAccessTokenTask => "Nop.Plugin.Payments.Square.Services.RenewAccessTokenTask";

    ///// <summary>
    ///// Default access token renewal period in days
    ///// </summary>
    //public static int AccessTokenRenewalPeriodRecommended => 14;

    ///// <summary>
    ///// Max access token renewal period in days
    ///// </summary>
    //public static int AccessTokenRenewalPeriodMax => 30;

    ///// <summary>
    ///// Sandbox credentials should start with this prefix
    ///// </summary>
    //public static string SandboxCredentialsPrefix => "sandbox-";

}
