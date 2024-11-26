using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configuration;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        //builder.HasData
        //(
        //    new Person
        //    {
        //        Id = new Guid("c9d4c053-49b6-410c-bc78-2d54a9991870"),
        //        Name = "",
        //        Surname = "",
        //        DayOfBirth = new DateTime(2000, 1, 1),
        //        DayOfNameDay = new DateTime(DateTime.Now.Year, 1, 1)
        //    }               
        //);
    }
}