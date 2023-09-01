using AutoMapper;
using Contracts;
using Entities.ConfigurationModels;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Service.Contracts;
using Shared.DTO;

namespace Service
{
    public class ServiceManager : IServiceManager
    {
        private readonly Lazy<ICompanyService> _companyService;
        private readonly Lazy<IEmployeeService> _employeeService;
        private readonly Lazy<IAuthenticationService> _authentificationService;

        public ServiceManager(IRepositoryManager repositoryManager
                            , ILoggerManager logger
                            , IMapper mapper
                            /*, IDataShaper<EmployeeDTO> dataShaper*/
                            ,IEmployeeLinks employeeLinks // data shaping inside
                            , UserManager<User> userManager
                            //, IConfiguration configuration
                            //,IOptions<JwtConfiguration> configuration
                            ,IOptionsSnapshot<JwtConfiguration> configuration
            )
        {
            _companyService = new Lazy<ICompanyService>(() => 
                    new CompanyService(repositoryManager, logger, mapper));
            _employeeService = new Lazy<IEmployeeService>(() =>
                    new EmployeeService(repositoryManager, logger, mapper, /*dataShaper*/ employeeLinks));
            _authentificationService = new Lazy<IAuthenticationService>(() =>
                    new AuthentificationService(logger, mapper, userManager, configuration));
        }
        public ICompanyService CompanyService => _companyService.Value;
        public IEmployeeService EmployeeService => _employeeService.Value;
        public IAuthenticationService AuthentificationService => _authentificationService.Value;
    }
}
