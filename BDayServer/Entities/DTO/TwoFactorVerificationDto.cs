using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTO
{
    public class TwoFactorVerificationDto
    {
        public string Email { get; set; }
        public string Provider { get; set; }
        [Required(ErrorMessage = "Token is required")]
        public string TwoFactorToken { get; set; }
    }
}
