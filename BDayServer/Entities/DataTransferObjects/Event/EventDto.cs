using System;

namespace Entities.DataTransferObjects.Event
{
    public class EventDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }       
        public DateTime Date { get; set; }
        public string ImageUrl { get; set; }
    }
}
