using AutoMapper;
using Entities;
using Entities.DataTransferObjects.Event;
using Entities.DataTransferObjects.Person;
using Entities.DataTransferObjects.User;
using Entities.Models;

namespace BDayServer;

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