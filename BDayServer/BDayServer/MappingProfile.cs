using AutoMapper;
using Entities;
using Entities.DataTransferObjects;
using Entities.DTO;
using Entities.Models;

namespace BDayServer
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Person, PersonDto>();        

            CreateMap<PersonForCreationDto, Person>();

            CreateMap<PersonForUpdateDto, Person>().ReverseMap();

            CreateMap<User, UserLite>();
        }
    }
}
