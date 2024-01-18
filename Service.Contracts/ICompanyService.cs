using Entities.Responses;
using Shared.DTO;

namespace Service.Contracts
{
    public interface ICompanyService
    {
        Task<IEnumerable<CompanyDTO>> GetAllCompaniesAsync(bool trackChanges);
        Task<CompanyDTO> GetCompanyByIdAsync(Guid Id, bool trackChanges);
        Task<CompanyDTO> CreateCompanyAsync(CompanyCreateDTO company);
        Task<IEnumerable<CompanyDTO>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges);
        Task<(IEnumerable<CompanyDTO> companies, string ids)> CreateCompanyCollectionAsync(IEnumerable<CompanyCreateDTO> companyCollection);
        Task DeleteCompanyAsync(Guid companyId, bool trackChanges);
        Task UpdateCompanyAsync(Guid companyId, CompanyForUpdateDTO companyForUpdate, bool trackChanges);

        // ApiBaseResponse allow us to return any type
        Task<ApiBaseResponse> GetAllCompanies(bool trackChanges);
        Task<ApiBaseResponse> GetCompanyById(Guid companyId, bool trackChanges);
    }
}
