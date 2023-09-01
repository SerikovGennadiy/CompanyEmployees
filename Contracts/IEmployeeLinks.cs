using Entities.LinkModels;
using Microsoft.AspNetCore.Http;
using Shared.DTO;

namespace Contracts
{
    public interface IEmployeeLinks
    {
        LinkResponse TryGenerateLinks(IEnumerable<EmployeeDTO> employeesDTO, 
                                                            string fields, 
                                                                Guid companyId, 
                                                                    HttpContext httpContext);
    }
}
