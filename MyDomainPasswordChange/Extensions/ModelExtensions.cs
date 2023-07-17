using MyDomainPasswordChange.Data.Models;
using MyDomainPasswordChange.Models;

namespace MyDomainPasswordChange.Extensions;

public static class ModelExtensions
{
    public static BlacklistedIpViewModel GetViewModel(this BlacklistedIpAddress model)
        => new()
        {
            Id = model.Id,
            AddedInBlacklist = model.AddedInBlacklist,
            IpAddress = model.IpAddress,
            Reason = model.Reason
        };

    public static BlacklistedIpAddress GetModel(this BlacklistedIpViewModel viewModel)
        => new()
        {
            Id = viewModel.Id,
            AddedInBlacklist = viewModel.AddedInBlacklist,
            IpAddress = viewModel.IpAddress,
            Reason = viewModel.Reason
        };
}