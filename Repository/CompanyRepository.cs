using Contracts;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Extensions;

namespace Repository
{
    public class CompanyRepository : RepositoryBase<Company>, ICompanyRepository
    {
        public CompanyRepository(RepositoryContext repositoryContext)
            : base(repositoryContext) { }


        public async Task<IEnumerable<Company>> GetAllCompaniesAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                    //.OrderBy(c => c.Name)
                        .ToListAsync();

        public async Task<Company> GetCompanyByIdAsync(Guid Id, bool trackChanges) =>
            await FindByCondition(c => c.Id.Equals(Id), trackChanges)
                    .SingleOrDefaultAsync();

        public void CreateCompany(Company company) => Create(company);

        public async  Task<IEnumerable<Company>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges) =>
           await FindByCondition(c => ids.Contains(c.Id), trackChanges)
                    .OrderBy(c => c.Name)
                        .ToListAsync();

        public void DeleteCompany(Company company) => Delete(company);
    }
}
