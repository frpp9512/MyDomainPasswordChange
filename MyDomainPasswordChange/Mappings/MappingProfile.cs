using AutoMapper;
using MyDomainPasswordChange.Management.Models;
using MyDomainPasswordChange.Models;

namespace MyDomainPasswordChange.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        _ = CreateMap<UserInfo, UserViewModel>();
        _ = CreateMap<UserInfo, SetUserPasswordViewModel>();
        _ = CreateMap<UserViewModel, UserInfo>();
        _ = CreateMap<GroupInfo, GroupModel>();
    }
}
