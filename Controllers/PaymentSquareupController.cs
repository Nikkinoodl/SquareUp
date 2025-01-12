using Nop.Plugin.Payments.SquareUp.Models;
using Nop.Web.Framework.Controllers;
using Nop.Services.Localization;
using Nop.Services.Common;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Configuration;
using Nop.Services.Stores;
using Nop.Core;
using Nop.Services.Logging;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Orders;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework;
using Nop.Services.Messages;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.SquareUp.Controllers;

public class PaymentSquareupController : BasePaymentController
{
private readonly IWorkContext _workContext;
private readonly IStoreService _storeService;
private readonly ISettingService _settingService;
private readonly IPaymentService _paymentService;
private readonly IOrderService _orderService;
private readonly IOrderProcessingService _orderProcessingService;
private readonly IGenericAttributeService _genericAttributeService;
private readonly ILocalizationService _localizationService;
private readonly ILogger _logger;
private readonly INotificationService _notificationService;
private readonly IWebHelper _webHelper;
private readonly PaymentSettings _paymentSettings;
private readonly SquareupPaymentSettings _squareupPaymentSettings;
private readonly ShoppingCartSettings _shoppingCartSettings;

public PaymentSquareupController(IWorkContext workContext,
    IStoreService storeService,
    ISettingService settingService,
    IPaymentService paymentService,
    IOrderService orderService,
    IOrderProcessingService orderProcessingService,
    IGenericAttributeService genericAttributeService,
    ILocalizationService localizationService,
    ILogger logger,
    INotificationService notificationService,
    IWebHelper webHelper,
    PaymentSettings paymentSettings,
    SquareupPaymentSettings squareupPaymentSettings,
    ShoppingCartSettings shoppingCartSettings)
{
    _workContext = workContext;
    _storeService = storeService;
    _settingService = settingService;
    _paymentService = paymentService;
    _orderService = orderService;
    _orderProcessingService = orderProcessingService;
    _genericAttributeService = genericAttributeService;
    _localizationService = localizationService;
    _logger = logger;
    _notificationService = notificationService;
    _webHelper = webHelper;
    _paymentSettings = paymentSettings;
    _squareupPaymentSettings = squareupPaymentSettings;
    _shoppingCartSettings = shoppingCartSettings;
}

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public async Task<IActionResult> Configure()
    {
        //load settings for a chosen store scope
        var squareupPaymentSettings = await _settingService.LoadSettingAsync<SquareupPaymentSettings>();

        var model = new ConfigurationModel
        {
            UseSandbox = squareupPaymentSettings.UseSandbox,
            SandboxAccessToken = squareupPaymentSettings.SandboxAccessToken,
            SandboxApplicationKey = squareupPaymentSettings.SandboxApplicationKey,
            SandboxLocationId = squareupPaymentSettings.SandboxLocationId,
            AccessToken = squareupPaymentSettings.AccessToken,
            ApplicationKey = squareupPaymentSettings.ApplicationKey,
            LocationId = squareupPaymentSettings.LocationId
        };

        return View("~/Plugins/Payments.SquareUp/Views/Configure.cshtml", model);
    }

    [HttpPost, ActionName("Configure")]
    [FormValueRequired("save")]
    [AuthorizeAdmin]
    //[AdminAntiForgery]
    [Area(AreaNames.ADMIN)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        var squareupPaymentSettings = await _settingService.LoadSettingAsync<SquareupPaymentSettings>();

        //save settings
        squareupPaymentSettings.UseSandbox =  model.UseSandbox;
        squareupPaymentSettings.SandboxAccessToken = model.SandboxAccessToken;
        squareupPaymentSettings.SandboxApplicationKey = model.SandboxApplicationKey;
        squareupPaymentSettings.SandboxLocationId = model.SandboxLocationId;
        squareupPaymentSettings.AccessToken = model.AccessToken;
        squareupPaymentSettings.ApplicationKey = model.ApplicationKey;
        squareupPaymentSettings.LocationId = model.LocationId;

        /* We do not clear cache after each setting update.
         * This behavior can increase performance because cached settings will not be cleared 
         * and loaded from database after each update */

         //Note that this payment method is currently intended for use only with a single store instance
        await _settingService.SaveSettingAsync(squareupPaymentSettings, x => x.UseSandbox);
        await _settingService.SaveSettingAsync(squareupPaymentSettings, x => x.SandboxAccessToken);
        await _settingService.SaveSettingAsync(squareupPaymentSettings, x => x.SandboxApplicationKey);
        await _settingService.SaveSettingAsync(squareupPaymentSettings, x => x.SandboxLocationId);
        await _settingService.SaveSettingAsync(squareupPaymentSettings, x => x.AccessToken);
        await _settingService.SaveSettingAsync(squareupPaymentSettings, x => x.ApplicationKey);
        await _settingService.SaveSettingAsync(squareupPaymentSettings, x => x.LocationId);

        //now clear settings cache
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }
}
