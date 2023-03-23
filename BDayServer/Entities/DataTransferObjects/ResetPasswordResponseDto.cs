using System.Collections.Generic;

namespace Entities.DataTransferObjects
{
    public class ResetPasswordResponseDto
    {
        public bool IsResetPasswordSuccessful { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}
