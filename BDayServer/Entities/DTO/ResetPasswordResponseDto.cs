using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTO
{
    public class ResetPasswordResponseDto
    {
        public bool IsResetPasswordSuccessful { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}
