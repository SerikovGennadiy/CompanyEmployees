namespace Shared.DTO
{
    public record CompanyForUpdateDTO(string Name, string Address, string Country
                                            , IEnumerable<EmployeeForCreationDTO>? Employees);
}
