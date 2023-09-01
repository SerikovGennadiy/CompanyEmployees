using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models
{
    public class Company
    {
        [Column("CompanyId")]
        public Guid Id { get; set; }

        [Required(ErrorMessage ="Company name is reqired field.")]
        [MaxLength(60, ErrorMessage ="Max compnay name length is 60 characters.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Address is reqired field.")]
        [MaxLength(60, ErrorMessage = "Max address length is 60 characters.")]
        public string? Address { get; set; }

        public string? Country { get; set; }

        public ICollection<Employee>? Employees { get; set; }
    }
}
