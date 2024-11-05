using AutoMapper;
using BDayClient.Pocos;
using Entities.Models;

namespace BDayClient;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        //CreateMap<Person, PersonForUpdateDto>();
        CreateMap<Person, PersonForUpdate>();
    }
}