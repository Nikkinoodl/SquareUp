using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.SquareUp.Models;
using Nop.Services.Localization;
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Payments.SquareUp.Components;

[ViewComponent(Name = "PaymentSquareUp")]
public class PaymentSquareUpViewComponent : ViewComponent
{
    #region Fields

    private string _applicationKey;
    private string _locationId;

    private readonly SquareupPaymentSettings _squareupPaymentSettings;

    #endregion

    #region Ctor

    public PaymentSquareUpViewComponent(
        SquareupPaymentSettings squareupPaymentSettings
        )
    {
        _squareupPaymentSettings = squareupPaymentSettings;
    }

    #endregion

    #region Methods

public IViewComponentResult Invoke()
{
        //Use sandbox settings only for development and testing
        if (_squareupPaymentSettings.UseSandbox)
        {
            _applicationKey = _squareupPaymentSettings.SandboxApplicationKey;
            _locationId = _squareupPaymentSettings.SandboxLocationId;
            
        }
        //all these transactions will be charged to a card
        else
        {
            _applicationKey = _squareupPaymentSettings.ApplicationKey;
            _locationId = _squareupPaymentSettings.LocationId;

        }

        //Pass ApplicationKey to the model
        PaymentInfoModel model = new()
        {
            ApplicationKey = _applicationKey,
            LocationId = _locationId
        };

        return View("~/Plugins/Payments.SquareUp/Views/PaymentInfo.cshtml", model);
    }

    #endregion
}