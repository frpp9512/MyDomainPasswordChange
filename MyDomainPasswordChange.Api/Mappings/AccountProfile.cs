using AutoMapper;
using MyDomainPasswordChange.Management.Models;
using MyDomainPasswordChange.Shared.DTO;
using MyDomainPasswordChange.Shared.Models;

namespace MyDomainPasswordChange.Api.Mappings;

public class AccountProfile : Profile
{
    public AccountProfile()
    {
        CreateMap<Account, UserInfo>().ReverseMap();
        CreateMap<AccountDto, UserInfo>().ReverseMap();
        CreateMap<CreateAccountDto, UserInfo>().ReverseMap();
        CreateMap<Group, GroupInfo>().ReverseMap();
        CreateMap<GroupInfo, GroupInfoDto>().ReverseMap();
    }
}
