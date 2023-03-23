using System;

namespace Entities.DataTransferObjects.Person
{
    public class PersonEmailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime DayOfBirth { get; set; }
        public DateTime DayOfNameDay { get; set; }
        public string ImageUrl { get; set; }
        public string PersonCreator { get; set; }
    }
}