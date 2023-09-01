namespace Service.Contracts
{
    // SERVICE GET INPUT DTO AND RETURN OUTPUT DTO!
    // Extract business logic from controlees,
    // Validation input parametrs
    // Cross-domain logic
    // Interpreter input output
    public interface IServiceManager
    {
        ICompanyService CompanyService { get; }
        IEmployeeService EmployeeService { get; }
        IAuthenticationService AuthentificationService { get; }
    }
}
