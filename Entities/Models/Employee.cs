using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Net;
using Entities.LinkModels;

namespace Entities.Models
{
    public class Employee
    {
        [Column("EmployeeId")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Employee name is required field.")]
        [MaxLength(30, ErrorMessage = "Max length of name is 30")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Age is required field.")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Position is required field.")]
        [MaxLength(20, ErrorMessage = "Max length of position is 30")]
        public string? Position { get; set; }

        [ForeignKey(nameof(Company))]
        public Guid CompanyId { get; set; }
        public Company? Company { get; set; }
    }
}
