using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DataTransferObjects
{
    public class PersonDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }       
        public string Surname { get; set; }    
        public DateTime DayOfBirth { get; set; }
        public DateTime DayOfNameDay { get; set; }
        public string ImageUrl { get; set; }
    }
}
