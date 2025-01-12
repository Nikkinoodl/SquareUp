using Square;
using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Directory;
using Nop.Services.Orders;
using Nop.Services.Directory;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Configuration;
using Nop.Services.Tax;
using Nop.Core;
using Nop.Services.Payments;
using Nop.Plugin.Payments.SquareUp.Controllers;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Orders;
using Nop.Services.Logging;
using Nop.Services.Customers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Nop.Services.Plugins;
using Square.Exceptions;
using Square.Models;
using Square.Apis;
using System.Threading.Tasks;
using Nop.Plugin.Payments.SquareUp.Components;
using Square.Authentication;

namespace Nop.Plugin.Payments.SquareUp;

/// <summary>
/// Implements the <cref='https://developer.squareup.com/reference/square' > api in nopCommerce
/// </summary>
public class SquareupPaymentProcessor : BasePlugin, IPaymentMethod
{
    #region Fields

    private string _accessToken;
    private Square.Environment _environment;
    private string _locationId;
    private object _objPaymentToken;

    private readonly CurrencySettings _currencySettings;
    private readonly IAddressService _addressService;
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly ICustomerService _customerService;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;
    private readonly IStateProvinceService _stateProvinceService;
    private readonly IWebHelper _webHelper;
    private readonly SquareupPaymentSettings _squareupPaymentSettings;

    #endregion

    #region Ctor

    public SquareupPaymentProcessor(CurrencySettings currencySettings,
        IAddressService addressService,
        ICountryService countryService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        ISettingService settingService,
        IStateProvinceService stateProvinceService,
        IWebHelper webHelper,
        SquareupPaymentSettings squareupPaymentSettings)
    {
        _currencySettings = currencySettings;
        _addressService = addressService;
        _countryService = countryService;
        _currencyService = currencyService;
        _customerService = customerService;
        _localizationService = localizationService;
        _settingService = settingService;
        _stateProvinceService = stateProvinceService;
        _webHelper = webHelper;
        _squareupPaymentSettings = squareupPaymentSettings;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Generates a unique key for the charge
    /// </summary>
    /// <returns>Guid</returns>
    public static string NewIdempotencyKey()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Gets a payment status
    /// </summary>
    /// <param name="state">SquareUp state</param>
    /// <returns>Payment status</returns>
    protected static PaymentStatus GetPaymentStatus(string state)
    {
        state ??= string.Empty;
        var result = PaymentStatus.Pending;

        switch (state.ToLowerInvariant())
        {
            case "pending":
                result = PaymentStatus.Pending;
                break;
            case "authorized":
                result = PaymentStatus.Authorized;
                break;
            case "captured":
            case "completed":
                result = PaymentStatus.Paid;
                break;
            case "expired":
            case "voided":
                result = PaymentStatus.Voided;
                break;
            case "refunded":
                result = PaymentStatus.Refunded;
                break;
            case "partially_refunded":
                result = PaymentStatus.PartiallyRefunded;
                break;
            default:
                break;
        }

        return result;
    }

    /// <summary>
    /// Creates an instance of the Square client class
    /// </summary>
    /// <param>none</param>
    /// <returns>SquareClient</returns>
    public SquareClient CreateSquareClient()
    {
        //Use sandbox mode for all development and testing
        if (_squareupPaymentSettings.UseSandbox)
        {
            _accessToken = _squareupPaymentSettings.SandboxAccessToken;
            _environment = Square.Environment.Sandbox;
        }
        //all these transactions will be charged to a card
        else
        {
            _accessToken = _squareupPaymentSettings.AccessToken;
            _environment = Square.Environment.Production;
        }

        //Create the Bearer Auth Model
        var authModel = new BearerAuthModel.Builder(
            accessToken: _accessToken)
            .Build();

        //Create the Square Client
        var client = new SquareClient.Builder()
            .Environment(_environment)
            .BearerAuthCredentials(authModel)
            .Build();
      
         return client;
    }

    /// <summary>
    /// Function returns the Square location id
    /// </summary>
    /// <param>none</param>
    /// <returns>LocationId</returns>
    public string GetLocation()
    {
        string locationId;

        //Use sandbox mode for all development and testing
        if (_squareupPaymentSettings.UseSandbox)
        {
            locationId = _squareupPaymentSettings.SandboxLocationId;

        }
        //all these transactions will be charged to a card
        else
        {
            locationId = _squareupPaymentSettings.LocationId;
        }

        return locationId;
    }


    /// <summary>
    /// Function creates an instance of a Square address
    /// </summary>
    /// <param>Nop.Core.Domain.Common.Address</param>
    /// <returns>Square.Models.Address</returns>
    public Square.Models.Address CreateAddress(Core.Domain.Common.Address a)
    {
        return a == null ? null : new Square.Models.Address
    (
        addressLine1: a.Address1,
        addressLine2: a.Address2,
        administrativeDistrictLevel1: _stateProvinceService.GetStateProvinceByIdAsync((int)a.StateProvinceId.GetValueOrDefault()).Result.Abbreviation,
        country: _countryService.GetCountryByIdAsync((int)a.CountryId.GetValueOrDefault()).Result.TwoLetterIsoCode,
        locality: a.City,
        postalCode: a.ZipPostalCode
    );
    }

    #endregion

    #region Methods

    /// <summary>
    /// Process a payment
    /// </summary>
    /// <param name="processPaymentRequest">Payment info required for an order processing</param>
    /// <returns>Process payment result</returns>
    public async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        var result = new ProcessPaymentResult();

        //Create an instance of SquareClient
        var client = CreateSquareClient();

       // Get the correct location Id
        _locationId = GetLocation();

        //Get the PaymentToken from the custom values dictionary
        if (processPaymentRequest.CustomValues.TryGetValue("PaymentToken", out _objPaymentToken))      
        {
            //get customer
            var customer = await _customerService.GetCustomerByIdAsync(processPaymentRequest.CustomerId) ?? throw new NopException("Customer cannot be loaded");

            //get the primary store currency
            var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId) ?? throw new NopException("Primary store currency cannot be loaded");

            //I've removed this bit because all transactions we process wil be in the primary store currency i.e. USD
            //so we don't need to check if the currency is supported by Square
            //if (!Enum.TryParse(currency.CurrencyCode, out Money.CurrencyEnum moneyCurrency))
            //    throw new NopException($"The {currency.CurrencyCode} currency is not supported by Square");

            //Use the CreateAddress utility to build the billing addresses in Square Models format.
            //They are all required fields in the OPC form
            var billingAddress = CreateAddress(await _addressService.GetAddressByIdAsync((int)customer.BillingAddressId));
            var shippingAddress = CreateAddress(await _addressService.GetAddressByIdAsync((int)customer.ShippingAddressId));
            var email = customer.Email;

            //remove any JSON formatting from payment token
            var paymentToken = Convert.ToString(_objPaymentToken);

            // Every payment you process with the SDK must have a unique idempotency key.
            // If you're unsure whether a particular payment succeeded, you can reattempt
            // it with the same idempotency key without worrying about double charging
            // the buyer.
            var uuid = NewIdempotencyKey();

            //Get the order total amount, round it to two digits and convert to cents.
            var totalCents = Convert.ToInt32(Math.Round(processPaymentRequest.OrderTotal, 2)*100);
            
            //The order amount must be passed to SquareUp in cents
            var paymentBodyMoney = new Money.Builder()
                  .Amount(totalCents)
                  .Currency(currency.CurrencyCode)
                  .Build();

            //Assemble the body
            //var body = new CreatePaymentRequest.Builder(paymentToken, uuid, paymentBodyMoney)
            //this first parameter here is supposed to be source ID, a new thing - I have yet to figure out what to use
            var body = new CreatePaymentRequest.Builder("1", uuid)
                .AmountMoney(paymentBodyMoney) //added this line
                .VerificationToken(paymentToken) //added this line
                .Autocomplete(true)
                .BuyerEmailAddress(email)
                .BillingAddress(billingAddress)
                .ShippingAddress(shippingAddress)
                .LocationId(_locationId)
                .Build();

            //Instantiate the Api and fire off the payment request to Square
            var paymentsApi = client.PaymentsApi;
            try
            {
                //Create payment using asynchronous method
                var paymentResponse = await paymentsApi.CreatePaymentAsync(body);

                if (paymentResponse.Errors == null) {
                    //the header used to return a transaction id that is needed for refunds
                    //but this seems to have been deprecated so we'll use payment id instead
                    result.NewPaymentStatus = PaymentStatus.Paid;

                    //Use CaptureTransactionId to hold the payment Id
                    //this will be needed in case of refunds
                    result.CaptureTransactionId = paymentResponse.Payment.Id;

                    //Remove the payment token from custom values as it is no longer needed
                    processPaymentRequest.CustomValues.Remove(paymentToken);

                    return result;

                }
                else
                {
                    //all errors must be handled
                    foreach (var e in paymentResponse.Errors)
                    {
                        result.AddError("SquareUp error : " + paymentResponse.Errors.ToString());
                    }
                    return result;
                }

            }
            catch (ApiException e)
            {
                foreach(var i in e.Errors)
                {
                    result.AddError("SquareUp Api error : " + i.ToString()+ " : " + i.Detail.ToString());
                }
                return result;
            }
        }
        //Payment token not found - payment cannot be processed
        else
        {
            result.AddError("This card could not be authorized. Please check the cc number, cvv, exp. date and zip code.");
            return result;
        }
    }

    /// <summary>
    /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
    /// </summary>
    /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
    public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
    {
        //not implemented for this payment type
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a value indicating whether payment method should be hidden during checkout
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <returns>true - hide; false - display.</returns>
    public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
    {
        //you can put any logic here
        //for example, hide this payment method if all products in the cart are downloadable
        //or hide this payment method if current customer is from certain country
        return Task.FromResult(false);
    }

    /// <summary>
    /// Gets additional handling fee
    /// </summary>
    /// <param name="cart">Shoping cart</param>
    /// <returns>Additional handling fee</returns>
    public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
    {
        //Additional handling fee is not supported
        return Task.FromResult(0.00M);
    }

    /// <summary>
    /// Captures payment
    /// </summary>
    /// <param name="capturePaymentRequest">Capture payment request</param>
    /// <returns>Capture payment result</returns>
    public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
    {
        return Task.FromResult(new CapturePaymentResult { Errors = _captureResult });
    }

    /// <summary>
    /// Refunds a payment
    /// </summary>
    /// <param name="refundPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public async Task<RefundPaymentResult> RefundAsync(Services.Payments.RefundPaymentRequest refundPaymentRequest)
    {
        var result = new RefundPaymentResult();

        //Create an instance of SquareClient
        var client = CreateSquareClient();

        // Every transaction with the SDK must have a unique idempotency key.
        // If you're unsure whether a particular payment succeeded, you can reattempt
        // it with the same idempotency key without worrying about double charging
        // the buyer.
        var uuid = NewIdempotencyKey();

        //get the primary store currency
        var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId) ?? throw new NopException("Primary store currency cannot be loaded");

        //Get the order total amount, round it to two digits and convert to cents.
        var totalCents = Convert.ToInt32(Math.Round(refundPaymentRequest.AmountToRefund, 2) * 100);

        //The order amount must be passed to SquareUp in cents
        var refundBodyMoney = new Money.Builder()
              .Amount(totalCents)
              .Currency(currency.CurrencyCode)
              .Build();

        //Get the paymentId from the Capture Transaction Id where we stored it
        var paymentId = refundPaymentRequest.Order.CaptureTransactionId;

        //var body = new Square.Models.RefundPaymentRequest.Builder(
        //        idempotencyKey: uuid,
        //        amountMoney: refundBodyMoney,
        //        //,
        //        paymentId: paymentId
        //        )
        //    .Build();

        var request = new Square.Models.RefundPaymentRequest(idempotencyKey: uuid, amountMoney: refundBodyMoney, paymentId: paymentId);


        //Instantiate the Api and fire off the refund request to Square
        var refundsApi = client.RefundsApi;

        try
        {
            //Create refund using asynchronous method
            var refundResult = await refundsApi.RefundPaymentAsync(request);

            if (refundResult.Errors == null)
            {

                if (refundPaymentRequest.AmountToRefund == refundPaymentRequest.Order.OrderTotal)
                {
                    //full refund was issued
                    result.NewPaymentStatus = PaymentStatus.Refunded;
                    return result;
                }
                else
                {
                    //partial refund was issued
                    result.NewPaymentStatus = PaymentStatus.PartiallyRefunded;
                    return result;
                }
            }
            else { 
                //all errors must be handled
                foreach (var e in refundResult.Errors)
                {
                    result.AddError("SquareUp error : " + refundResult.Errors.ToString());
                }
            }
            return result;
        }
        catch (ApiException e)
        {
            foreach (var i in e.Errors)
            {
                result.AddError("SquareUp Api error : " + i.ToString() + " : " + i.Detail.ToString());
            }
            return result;
        };
    }

    /// <summary>
    /// Voids a payment
    /// </summary>
    /// <param name="voidPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
    {
        return Task.FromResult(new VoidPaymentResult { Errors = _voidResult });
    }

    /// <summary>
    /// Process recurring payment
    /// </summary>
    /// <param name="processPaymentRequest">Payment info required for an order processing</param>
    /// <returns>Process payment result</returns>
    public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult { Errors = _recurringResult });
    }

    /// <summary>
    /// Cancels a recurring payment
    /// </summary>
    /// <param name="cancelPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
    {
        return Task.FromResult(new CancelRecurringPaymentResult { Errors = _recurringResult });

    }

    /// <summary>
    /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
    /// </summary>
    /// <param name="order">Order</param>
    /// <returns>Result</returns>
    public Task<bool> CanRePostProcessPaymentAsync(Core.Domain.Orders.Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        //it's not a redirection payment method. So we always return false
        return Task.FromResult(false);
    }

    /// <summary>
    /// Gets a route for payment info
    /// </summary>
    /// <param name="actionName">Action name</param>
    /// <param name="controllerName">Controller name</param>
    /// <param name="routeValues">Route values</param>
    public static void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
    {
        actionName = "PaymentInfo";
        controllerName = "PaymentsSquareup";
        routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.SquareUp.Controllers" }, { "area", null } };
    }

    /// <summary>
    /// Validate payment form
    /// </summary>
    /// <param name="form">The parsed form values</param>
    /// <returns>List of validating errors</returns>
    public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
    {
        //try to get errors
        if (form.TryGetValue("Errors", out var errorsString) && !StringValues.IsNullOrEmpty(errorsString))
            return Task.FromResult<IList<string>>(errorsString.ToString().Split(_separator, StringSplitOptions.RemoveEmptyEntries).ToList());
        else
            return Task.FromResult<IList<string>>(new List<string>());
    }

    /// <summary>
    /// Get payment information
    /// </summary>
    /// <param name="form">The parsed form values</param>
    /// <returns>Payment info holder</returns>
    public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
    {

        var paymentInfo = new ProcessPaymentRequest();

        //Read the value of the paymenttoken input in the form, clean up and JSON formatting elements
        //and store it in the CustomValues dictionary          
        var paymentToken = form["paymenttoken"].ToString().Replace("[", string.Empty).Replace("]", string.Empty).Trim().Trim('"');

        //Check that a payment token has been returned.  If not, the card was declined so we will leave the dictionary empty
        //so that TryGetValue fails in payment processing and an error is returned.
        if (string.IsNullOrEmpty(paymentToken) == false)
        {
            paymentInfo.CustomValues.Add("PaymentToken", paymentToken);
        }

        return Task.FromResult(paymentInfo);
    }

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/PaymentSquareup/Configure";
    }

    /// <summary>
    /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
    /// </summary>
    /// <returns>View component name</returns>
    public Type GetPublicViewComponent()
    {
        return typeof(PaymentSquareUpViewComponent);
    }

    /// <summary>
    /// Install the plugin
    /// </summary>
    public override async Task InstallAsync()
    {
        //default settings
        var settings = new SquareupPaymentSettings
        {
            UseSandbox = true,
        };
        await _settingService.SaveSettingAsync(settings);

        //configuration

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string> {
            ["Plugins.Payments.Squareup.Fields.UseSandbox"] = "Use Sandbox",
            ["Plugins.Payments.Squareup.Fields.UseSandbox.Hint"] = "Check to enable Sandbox testing environment",
            ["Plugins.Payments.SquareUp.Fields.SandboxAccessToken"] = "Sandbox access token",
            ["Plugins.Payments.SquareUp.Fields.SandboxApplicationKey"] = "Sandbox application key",
            ["Plugins.Payments.SquareUp.Fields.SandboxLocationId"] = "Sandbox location id",
            ["Plugins.Payments.SquareUp.Fields.AccessToken"] = "Access token for your account",
            ["Plugins.Payments.SquareUp.Fields.LocationId"] = "Store location Id",

            //payment description
            ["Plugins.Payments.Squareup.PaymentMethodDescription"] = "Payment processing by SquareUp",

            //payment info labels
            ["Plugins.Payments.SquareUp.Fields.CardNumber"] = "Credit card number",
            ["Plugins.Payments.SquareUp.Fields.Cvv"] = "Card security code",
            ["Plugins.Payments.SquareUp.Fields.ExpirationDate"] = "Expiration date",
            ["Plugins.Payments.SquareUp.Fields.PostalCode"] = "Billing zip code"
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall the plugin
    /// </summary>
    public override async Task UninstallAsync()
    {
        //settings
        await _settingService.DeleteSettingAsync<SquareupPaymentSettings>();

        //locales
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.SquareUp");

        await base.UninstallAsync();
    }
    #endregion

    #region Properties

    /// <summary>
    /// Get type of controller
    /// </summary>
    /// <returns>Type</returns>
    public static Type GetControllerType()
    {
        return typeof(PaymentSquareupController);
    }

    /// <summary>
    /// Gets a value indicating whether capture is supported
    /// </summary>
    public bool SupportCapture => false;

    /// <summary>
    /// Gets a value indicating whether partial refund is supported
    /// </summary>
    public bool SupportPartiallyRefund => true;

    /// <summary>
    /// Gets a value indicating whether refund is supported
    /// </summary>
    public bool SupportRefund => true;

    /// <summary>
    /// Gets a value indicating whether void is supported
    /// </summary>
    public bool SupportVoid => false;

    /// <summary>
    /// Gets a recurring payment type of payment method
    /// </summary>
    public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

    /// <summary>
    /// Gets a payment method type
    /// </summary>
    public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

    /// <summary>
    /// Gets a value indicating whether we should display a payment information page for this plugin
    /// </summary>
    public bool SkipPaymentInfo => false;

    private static readonly string[] _captureResult= new[] { "Capture method not supported" };
    private static readonly string[] _voidResult = new[] { "Void method not supported" };
    private static readonly string[] _recurringResult = new[] { "Recurring payments not supported" };
    private static readonly char[] _separator = new[] { '|' };

    /// <summary>
    /// Gets a payment method description that will be displayed on checkout pages in the public store
    /// </summary>
    public async Task<string> GetPaymentMethodDescriptionAsync()
    {
        return await _localizationService.GetResourceAsync("Plugins.Payments.SquareUp.PaymentMethodDescription");
    }

    #endregion
}
