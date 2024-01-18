using Contracts;
using Shared.DTO;
using Service.Contracts;
using AutoMapper;
using Entities.Excepions;
using Entities.Models;
using Entities.Responses;

namespace Service
{
    public sealed class CompanyService : ICompanyService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        public CompanyService(IRepositoryManager repositoryManager, ILoggerManager loggerManager, IMapper mapper)
        {
            _repository = repositoryManager;
            _logger = loggerManager;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CompanyDTO>> GetAllCompaniesAsync(bool trackChanges)
        {
            // comments due to centralized exception handling
            //try
            //{
                var companies = await _repository.Company.GetAllCompaniesAsync(trackChanges);

                //var companiesDTO = companies
                //                        .Select(c => 
                //                            new CompanyDTO(c.Id, c.Name ?? string.Empty, string.Join(" ", c.Country, c.Address)))
                //                        .ToList();

                var companiesDTO = _mapper.Map<IEnumerable<CompanyDTO>>(companies);
                return companiesDTO;
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError($"Something went wrong in {nameof(GetAllCompanies)} service method {ex}");
            //    throw;
            //}
        }

        public async Task<CompanyDTO> GetCompanyByIdAsync(Guid Id, bool trackChanges)
        {
            Company? company = await GetCompanyAndCheckIfItExists(Id, trackChanges);

            var companyDTO = _mapper.Map<CompanyDTO>(company);
            return companyDTO;
        }  

        public async Task<CompanyDTO> CreateCompanyAsync(CompanyCreateDTO company)
        {
            var companyEntity = _mapper.Map<Company>(company);

            _repository.Company.CreateCompany(companyEntity);
            await _repository.SaveAsync();

            var companyToReturn = _mapper.Map<CompanyDTO>(companyEntity);
            return companyToReturn;
        }

        public async Task<IEnumerable<CompanyDTO>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges)
        { 
            if (ids is null)
                throw new IdParametrBadRequestException();
            
            var companyEntities = await _repository.Company.GetByIdsAsync(ids, trackChanges);
            if (ids.Count() != companyEntities.Count())
                throw new CollectionsByIdsBadRequestException();

            var companiesToReturn = _mapper.Map<IEnumerable<CompanyDTO>>(companyEntities);
            return companiesToReturn;
        }

        public async Task<(IEnumerable<CompanyDTO> companies, string ids)> CreateCompanyCollectionAsync
            (IEnumerable<CompanyCreateDTO> companyCollection)
        {
            if (companyCollection is null)
                throw new CompanyCollectionBadRequestException();

            var companies = _mapper.Map<IEnumerable<Company>>(companyCollection);
            foreach (var company in companies)
            {
                _repository.Company.CreateCompany(company);
            }
            await _repository.SaveAsync();

            var companyCollectionToReturn = _mapper.Map<IEnumerable<CompanyDTO>>(companies);
            var ids = string.Join(",", companyCollectionToReturn.Select(c => c.Id));

            return (companies: companyCollectionToReturn, ids: ids);
        }

        public async Task DeleteCompanyAsync(Guid companyId, bool trackChanges)
        {
            Company? company = await GetCompanyAndCheckIfItExists(companyId, trackChanges);

            _repository.Company.DeleteCompany(company);
            await _repository.SaveAsync();
        }

        public async Task UpdateCompanyAsync(Guid companyId, CompanyForUpdateDTO companyForUpdate, bool trackChanges)
        {
            Company? companyEntity = await GetCompanyAndCheckIfItExists(companyId, trackChanges);

            _mapper.Map(companyForUpdate, companyEntity);
            await _repository.SaveAsync();
        }

        private async Task<Company?> GetCompanyAndCheckIfItExists(Guid id, bool trackChanges)
        {
            var company = await _repository.Company.GetCompanyByIdAsync(id, trackChanges: trackChanges);
            if (company is null)
                throw new CompanyNotFoundException(companyId: id);
            return company;
        }

        public ApiBaseResponse GetAllCompanies(bool trackChanges)
        {
            var companies = _repository.Company.GetAllCompaniesAsync(trackChanges);
            var companiesDTO = _mapper.Map<IEnumerable<CompanyDTO>>(companies);
            return new ApiOkResponse<IEnumerable<CompanyDTO>>(companiesDTO);
        }
        /* !!!!
            Here we are not use exception class to return error result (CompanyNotFoundException)
            With ApiBaseResponse abstraction, we are safe to return multiple types from our method,
            as long as they inherit from ApiBaseResponse abstract class.
            Here we can use _logger for some messages
         */
        public ApiBaseResponse GetCompanyById(Guid companyId, bool trackChanges)
        {
            var company = _repository.Company.GetCompanyByIdAsync(companyId, trackChanges);
            if (company is null)
                return new CompanyNotFoundResponse(companyId);

            var companyDTO = _mapper.Map<CompanyDTO>(company);
            return new ApiOkResponse<CompanyDTO>(companyDTO);
        }
    }
}
