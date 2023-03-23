using System.Collections.Generic;

namespace Entities.DataTransferObjects
{
    public class ResponseDto
    {
        public bool IsSuccessfulRegistration { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}
