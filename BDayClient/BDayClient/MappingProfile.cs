using AutoMapper;
using Entities.DataTransferObjects.Person;
using Entities.Models;

namespace BDayClient
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Person, PersonForUpdateDto>();
        }
    }
}
