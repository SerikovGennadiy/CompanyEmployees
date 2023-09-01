using Entities;
using Entities.LinkModels;
using Entities.Models;
using Shared.DTO;
using Shared.RequestFeatures;
using System.Dynamic;

namespace Service.Contracts
{
    public interface IEmployeeService
    {
        // 1th version before paging
        //Task<IEnumerable<EmployeeDTO>> GetEmployeesAsync(Guid companyId, 
        //    EmployeeParameters employeeParameters, bool trackChanges);

        // 2th version after paging before shaping
        //Task<(IEnumerable<EmployeeDTO> employees, MetaData metaData)> GetEmployeesAsync(Guid companyId,
        //    EmployeeParameters employeeParameters, bool trackChages);

        //3th version after shaping
        //Task<(IEnumerable</*ExpandoObject Entity*/ShapedEntity> employees, MetaData metaData)> GetEmployeesAsync(Guid companyId,
        //    EmployeeParameters employeeParameters, bool trackChanges);
       
        //4th version HATEOUS
        Task<(LinkResponse linkResponse, MetaData metaData)> GetEmployeesAsync(Guid companyId,
            LinkParameters linkParameters, bool trackChanges);

        Task<EmployeeDTO> GetEmployeeAsync(Guid compnayId, Guid employeeId, bool trackChanges);
        Task<EmployeeDTO> CreateEmployeeForCompanyAsync(Guid companyId,
                                                EmployeeForCreationDTO employeeForCreationDTO,
                                                     bool trackChanges);
        Task DeleteEmployeeForCompanyAsync(Guid companyId, Guid id, bool trackChanges);
        Task UpdateEmployeeForCompanyAsync(Guid companyId, Guid id, EmployeeForUpdateDTO employeeForUpdate,
                                        bool compTrackChanges, bool empTrackChanges);

        Task<(EmployeeForUpdateDTO employeeToPatch, Employee employeeEntity)> GetEmployeeForPatchAsync(
            Guid companyId, Guid id, bool compTrackChanges, bool empTrackChanges);

        Task SaveChangesForPatchAsync(EmployeeForUpdateDTO employeeToPatch, Employee employeeEntity);
    }
    
}
