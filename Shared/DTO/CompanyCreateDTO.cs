namespace Shared.DTO
{
    public record CompanyCreateDTO (string Name, string Address, string Country,
                                    IEnumerable<EmployeeForCreationDTO>? Employees);
}
