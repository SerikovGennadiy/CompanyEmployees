using Contracts;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Extensions;
using Shared.RequestFeatures;

namespace Repository
{
    public class EmployeeRepository : RepositoryBase<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(RepositoryContext repositoryContext)
            : base(repositoryContext) { }

        //public async Task<IEnumerable<Employee>> GetEmployeesAsync(Guid companyId, 
        //    EmployeeParameters employeeParameters, bool trackChanges) =>
        //   await FindByCondition(e => e.CompanyId.Equals(companyId), trackChanges)
        //            .OrderBy(e => e.Name)
        //            .Skip((employeeParameters.PageNumber - 1) * employeeParameters.PageSize)
        //            .Take(employeeParameters.PageSize)
        //                .ToListAsync();

        // EmployeeParameters - about searching, filtering, sorting, data shaping
        public async Task<PagedList<Employee>> GetEmployeesAsync(Guid companyId,
          EmployeeParameters employeeParameters, bool trackChanges)
        {
            var employees = await FindByCondition(e => e.CompanyId.Equals(companyId), trackChanges)
                                  .FilterEmployees(employeeParameters.MinAge, employeeParameters.MaxAge)
                                  .Search(employeeParameters.SearchTerm)
                                  //.OrderBy(e => e.Name)
                                  .Sort(employeeParameters.OrderBy)
                                  .ToListAsync();

            return PagedList<Employee>
                        .ToPagedList(employees, employeeParameters.PageNumber, employeeParameters.PageSize);
        }

        // SECOND WAY. ITS MORE USEFUL FOR MILLIONS ROWS
        public async Task<PagedList<Employee>> GetEmployees2(Guid companyId,
            EmployeeParameters employeeParameters, bool trackChanges)
        {
            var employees = await FindByCondition(e => e.CompanyId.Equals(companyId), trackChanges)
                .OrderBy(e => e.Name)
                .Skip((employeeParameters.PageNumber - 1) * employeeParameters.PageSize)
                .Take(employeeParameters.PageSize)
                .ToListAsync();

            // Notwithstanding same request two times its tested many times in millions row db
            // was faster when abouve mentioned
            var count = await FindByCondition(e => e.CompanyId.Equals(companyId), trackChanges)
                                .CountAsync();

            return new PagedList<Employee>(employees, 
                        count, 
                        employeeParameters.PageNumber, 
                        employeeParameters.PageSize);
        }

//------------------------------------------------------------------------------------

        public async Task<Employee> GetEmployeeAsync(Guid companyId, Guid employeeId, bool trackChanges) =>
            await FindByCondition(e => e.CompanyId.Equals(companyId)
                              && e.Id.Equals(employeeId), trackChanges)
                                .SingleOrDefaultAsync();

        public void CreateEmployeeForCompany(Guid companyId, Employee employee)
        {
            employee.CompanyId = companyId;
            Create(employee);
        }

        public void DeleteEmployee(Employee employee) => Delete(employee);
    }
}
