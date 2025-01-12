using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.ScheduleTasks;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.UI;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.SquareUp;

/// <summary>
/// Represents plugin event consumer
/// </summary>
public class EventConsumer :
    IConsumer<PageRenderingEvent>,
    IConsumer<ModelReceivedEvent<BaseNopModel>>
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly IPaymentPluginManager _paymentPluginManager;
    private readonly IScheduleTaskService _scheduleTaskService;
    private readonly SquareupPaymentSettings _squareupPaymentSettings;

    #endregion

    #region Ctor

    public EventConsumer(ILocalizationService localizationService,
        IPaymentPluginManager paymentPluginManager,
        IScheduleTaskService scheduleTaskService,
        SquareupPaymentSettings squareupPaymentSettings)
    {
        _localizationService = localizationService;
        _paymentPluginManager = paymentPluginManager;
        _scheduleTaskService = scheduleTaskService;
        _squareupPaymentSettings = squareupPaymentSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Handle page rendering event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    public async Task HandleEventAsync(PageRenderingEvent eventMessage)
    {
        //check whether the plugin is active
        if (!await _paymentPluginManager.IsPluginActiveAsync(SquareupPaymentDefaults.SystemName))
            return;

        //add js script to one page checkout
        if (eventMessage.GetRouteName()?.Equals(SquareupPaymentDefaults.OnePageCheckoutRouteName) ?? false)
        {
            eventMessage.Helper?.AddScriptParts(ResourceLocation.Head,
                _squareupPaymentSettings.UseSandbox ? SquareupPaymentDefaults.SandboxPaymentFormScriptPath : SquareupPaymentDefaults.PaymentFormScriptPath,
                excludeFromBundle: true);
        }
    }

    /// <summary>
    /// Handle model received event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    public Task HandleEventAsync(ModelReceivedEvent<BaseNopModel> eventMessage)
    {
        //not implemented for this plugin
        return Task.CompletedTask;
    }

    #endregion
}
