using AutoMapper;
using MyDomainPasswordChange.Api.Models;
using MyDomainPasswordChange.Shared.DTO;

namespace MyDomainPasswordChange.Api.Mappings;

public class GlobalProfile : Profile
{
    public GlobalProfile()
    {
        CreateMap<DependencyDefinition, DependencyDto>().ForMember(d => d.Areas, opt => opt.MapFrom(d => d.AreaDefinitions));
        CreateMap<AreaDefinition, AreaDto>();
    }
}
