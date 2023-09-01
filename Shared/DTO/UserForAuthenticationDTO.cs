using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTO
{
    public record UserForAuthenticationDTO
    {
        [Required(ErrorMessage = "User name is required field")]
        public string? UserName { get; init; }

        [Required(ErrorMessage = "Password name is required field")]
        public string? Password { get; init; }
    }
}
