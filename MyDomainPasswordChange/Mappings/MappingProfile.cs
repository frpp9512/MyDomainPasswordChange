using AutoMapper;
using MyDomainPasswordChange.Management.Models;
using MyDomainPasswordChange.Models;

namespace MyDomainPasswordChange.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserInfo, UserViewModel>();
        CreateMap<UserInfo, SetUserPasswordViewModel>();
        CreateMap<UserViewModel, UserInfo>();
    }
}
