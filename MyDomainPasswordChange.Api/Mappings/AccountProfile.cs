using AutoMapper;
using MyDomainPasswordChange.Management.Models;
using MyDomainPasswordChange.Shared.DTO;
using MyDomainPasswordChange.Shared.Models;

namespace MyDomainPasswordChange.Api.Mappings;

public class AccountProfile : Profile
{
    public AccountProfile()
    {
        _ = CreateMap<Account, UserInfo>().ReverseMap();
        _ = CreateMap<AccountDto, UserInfo>().ReverseMap();
        _ = CreateMap<CreateAccountDto, UserInfo>().ReverseMap();
        _ = CreateMap<Group, GroupInfo>().ReverseMap();
        _ = CreateMap<GroupInfo, GroupInfoDto>().ReverseMap();
    }
}
