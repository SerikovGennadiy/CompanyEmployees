namespace Entities.Excepions
{
    public sealed class EmployeeNotFoundException : NotFoundException
    {
        public EmployeeNotFoundException(Guid employeeId)
            : base($"Employee with id: {employeeId} doesn't exist in database") { }
    }
}
