using Nop.Web.Framework.Models;
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Payments.SquareUp.Models;

public record PaymentInfoModel : BaseNopModel
{
    [StringLength(100)]
    public string ApplicationKey { get; set; }
    public string LocationId { get; set; }
}