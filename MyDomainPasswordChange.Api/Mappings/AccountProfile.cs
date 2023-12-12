using AutoMapper;
using MyDomainPasswordChange.Management.Models;
using MyDomainPasswordChange.Shared.DTO;
using MyDomainPasswordChange.Shared.Models;

namespace MyDomainPasswordChange.Api.Mappings;

public class AccountProfile : Profile
{
    public AccountProfile()
    {
        CreateMap<Account, UserInfo>();
        CreateMap<AccountDto, UserInfo>();
        CreateMap<CreateAccountDto, UserInfo>();
    }
}
