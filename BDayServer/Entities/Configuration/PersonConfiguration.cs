using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Configuration
{
    public class PersonConfiguration : IEntityTypeConfiguration<Person>
    {
        public void Configure(EntityTypeBuilder<Person> builder)
        {
            builder.HasData
            (
                new Person
                {
                    Id = new Guid("c9d4c053-49b6-410c-bc78-2d54a9991870"),
                    Name = "Matúš",
                    Surname = "Gramblička",
                    DayOfBirth = new DateTime(1988, 5, 27),
                    DayOfNameDay = new DateTime(DateTime.Now.Year, 9, 29)
                },
                new Person
                {
                    Id = new Guid("3d490a70-94ce-4d15-9494-5248280c2ce3"),
                    Name = "Diana",
                    Surname = "Grambličková",
                    DayOfBirth = new DateTime(1992, 2, 1),
                    DayOfNameDay = new DateTime(DateTime.Now.Year, 7, 1)
                }
            );
        }
    }
}
