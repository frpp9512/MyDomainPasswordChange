using MyDomainPasswordChange.Data.Models;
using MyDomainPasswordChange.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Extensions
{
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
}