using AutoMapper;
using Contracts.DataTransferObjects.Event;
using Contracts.DataTransferObjects.Person;
using Contracts.DataTransferObjects.User;
using Entities.Models;

namespace BDayServer.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Person, PersonDto>();    
        CreateMap<PersonForCreationDto, Person>();
        CreateMap<PersonForUpdateDto, Person>().ReverseMap();
        CreateMap<Person, PersonEmailDto>();

        CreateMap<User, UserLite>();

        CreateMap<Event, EventDto>();
        CreateMap<EventForCreationDto, Event>();
        CreateMap<EventForUpdateDto, Event>().ReverseMap();
        CreateMap<Event, EventEmailDto>();
    }
}