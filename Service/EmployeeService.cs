using AutoMapper;
using Contracts;
using Entities;
using Entities.Excepions;
using Entities.Exceptions;
using Entities.LinkModels;
using Entities.Models;
using Service.Contracts;
using Service.DataShaping;
using Shared.DTO;
using Shared.RequestFeatures;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace Service
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IRepositoryManager _repositoryManager;
        private readonly ILoggerManager _loggerManager;
        private readonly IMapper _mapper;
        //private readonly IDataShaper<EmployeeDTO> _dataShaper;
        private readonly IEmployeeLinks _employeeLinks;
        public EmployeeService(IRepositoryManager repositoryManager
                             , ILoggerManager loggerManager
                             , IMapper mapper
                             /*, IDataShaper<EmployeeDTO> dataShaper*/
                             , IEmployeeLinks employeeLinks)
        {
            _repositoryManager = repositoryManager;
            _loggerManager = loggerManager;
            _mapper = mapper;
            //_dataShaper = dataShaper;
            _employeeLinks = employeeLinks;
        }
//----------------------- skip take --------------------
        //public async Task<IEnumerable<EmployeeDTO>> GetEmployeesAsync(Guid companyId, 
        //    EmployeeParameters employeeParameters, bool trackChanges)
        //{
        //    await CheckIfCompanyExists(companyId, trackChanges);

        //    var employeesFromDb = await _repositoryManager.Employee.GetEmployeesAsync(companyId, employeeParameters, trackChanges);
        //    var employeeDTOs = _mapper.Map<IEnumerable<EmployeeDTO>>(employeesFromDb);
        //    return employeeDTOs;

        //}
       // public async Task<(IEnumerable<EmployeeDTO> employees, MetaData metaData)> GetEmployeesAsync(Guid companyId,
        
        // SHAPING DATA
        //public async Task<(IEnumerable</*ExpandoObject Entity*/ShapedEntity> employees, MetaData metaData)> GetEmployeesAsync(Guid companyId,
        //    EmployeeParameters employeeParameters, bool trackChanges)
        //{
        //    if (!employeeParameters.ValidAgeRange)
        //        throw new MaxAgeRangeBadRequestException();

        //    await CheckIfCompanyExists(companyId, trackChanges);

        //    // this name more suitable, because variable has metadata yet
        //    var employeesWithMetaData = await _repositoryManager.Employee
        //        .GetEmployeesAsync(companyId, employeeParameters, trackChanges);
        //    var employeesDTO = _mapper.Map<IEnumerable<EmployeeDTO>>(employeesWithMetaData);

        //    // shaped data by ctor.dataShaper
        //    var shapedData = _dataShaper.ShapeData(employeesDTO, employeeParameters.Fields);

        //    // after mapping create tuple and return caller
        //   // return (employees: employeesDTO, metaData: employeesWithMetaData.MetaData);
        //    return (employees: shapedData, metaData: employeesWithMetaData.MetaData);
        //}
        // HATEOUS
        public async Task<(LinkResponse linkResponse, MetaData metaData)> GetEmployeesAsync(Guid companyId,
            LinkParameters linkParameters, bool trackChanges)
        {
            if (!linkParameters.EmployeeParameters.ValidAgeRange)
                throw new MaxAgeRangeBadRequestException();

            await CheckIfCompanyExists(companyId, trackChanges);

            // DATA SHAPER INSIDE EmployeeLinks
            var employeesWithMetaData = await _repositoryManager.Employee
                .GetEmployeesAsync(companyId, linkParameters.EmployeeParameters, trackChanges);

            var employeesDTO = _mapper.Map<IEnumerable<EmployeeDTO>>(employeesWithMetaData);

            // return links:LinkResponse as result TryGenerateLinks
            var links = _employeeLinks
                            .TryGenerateLinks(employeesDTO
                                            , linkParameters.EmployeeParameters.Fields
                                            , companyId
                                            , linkParameters.context);

            return (linkResponse: links, metaData: employeesWithMetaData.MetaData);
        }
        //--------------------------------------------------------------------------------

        public async Task<EmployeeDTO> GetEmployeeAsync(Guid companyId, Guid employeeId, bool trackChanges)
        {
            await CheckIfCompanyExists(companyId, trackChanges);

            var employeeDb = await GetEmployeeForCompanyAndCheckIfItExists(companyId, employeeId, trackChanges);
           
            var employee = _mapper.Map<EmployeeDTO>(employeeDb);
            return employee;
        }

        public async Task<EmployeeDTO> CreateEmployeeForCompanyAsync(Guid companyId, EmployeeForCreationDTO employeeForCreation, bool trackChanges)
        {
            await CheckIfCompanyExists(companyId, trackChanges);

            var employeeEntity = _mapper.Map<Employee>(employeeForCreation);

            _repositoryManager.Employee.CreateEmployeeForCompany(companyId, employeeEntity);
            await _repositoryManager.SaveAsync();

            var employeeToReturn = _mapper.Map<EmployeeDTO>(employeeEntity);
            return employeeToReturn;
        }

        public async Task DeleteEmployeeForCompanyAsync(Guid companyId, Guid id, bool trackChanges)
        {
            await CheckIfCompanyExists(companyId, trackChanges);

            var employeeForCompany = await GetEmployeeForCompanyAndCheckIfItExists(companyId, id, trackChanges);

            _repositoryManager.Employee.DeleteEmployee(employeeForCompany);
            await _repositoryManager.SaveAsync();
        }

        public async Task UpdateEmployeeForCompanyAsync(Guid companyId, Guid id, EmployeeForUpdateDTO employeeForUpdate, bool compTrackChanges, bool empTrackChanges)
        {
            await CheckIfCompanyExists(companyId, compTrackChanges);

            var employeeEntity = await GetEmployeeForCompanyAndCheckIfItExists(companyId, id, empTrackChanges);

            _mapper.Map(employeeForUpdate, employeeEntity);
            await _repositoryManager.SaveAsync();
        }

        public async Task<(EmployeeForUpdateDTO employeeToPatch, Employee employeeEntity)> GetEmployeeForPatchAsync(Guid companyId, Guid id, bool compTrackChanges, bool empTrackChanges)
        {
            await CheckIfCompanyExists(companyId, compTrackChanges);

            var employeeEntity = await GetEmployeeForCompanyAndCheckIfItExists(companyId, id, empTrackChanges);

            var employeeToPatch = _mapper.Map<EmployeeForUpdateDTO>(employeeEntity);

            return (employeeToPatch, employeeEntity);
        }

        public async Task SaveChangesForPatchAsync(EmployeeForUpdateDTO employeeToPatch, Employee employeeEntity)
        {
            _mapper.Map(employeeToPatch, employeeEntity);
            await _repositoryManager.SaveAsync();
        }

        private async Task CheckIfCompanyExists(Guid companyId, bool trackChanges)
        {
            var company = await _repositoryManager.Company.GetCompanyByIdAsync(companyId, trackChanges);
            if (company is null)
                throw new CompanyNotFoundException(companyId);
        }

        private async Task<Employee> GetEmployeeForCompanyAndCheckIfItExists(Guid companyId, Guid id, bool trackChanges)
        {
            var employeeDb = await _repositoryManager.Employee.GetEmployeeAsync(companyId, id, trackChanges);
            if (employeeDb is null)
                throw new EmployeeNotFoundException(id);

            return employeeDb;
        }
    }
}
