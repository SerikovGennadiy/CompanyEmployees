using Contracts;
//using Entities;
//using Entities.LinkModels;
using Shared.DTO;
using Microsoft.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using Entities.LinkModels;
using Entities;

namespace CompanyEmployees.Utility
{
    public class EmployeeLinks : IEmployeeLinks
    {
        public readonly LinkGenerator _linkGenerator;
        public readonly IDataShaper<EmployeeDTO> _dataShaper;

        public EmployeeLinks(LinkGenerator linkGenerator, 
                                IDataShaper<EmployeeDTO> dataShaper)
        {
            _linkGenerator = linkGenerator;
            _dataShaper = dataShaper;
        }

        public LinkResponse TryGenerateLinks(IEnumerable<EmployeeDTO> employeesDTO, 
                                                                string fields, // for data shaping
                                                                    Guid companyId, // for routes to CONCRETE this employee contain companyId
                                                                        HttpContext httpContext) // hold info about media type
        {
            var shapedEmployees = ShapeData(employeesDTO, fields);

            if(ShouldGenerateLinks(httpContext))
            {
                // return response HATEOUS-enriched
                return ReturnLinkedEmployees(employeesDTO, fields, companyId, httpContext, shapedEmployees);
            }

            // return simple shaped data
            return ReturnedShapedEmployees(shapedEmployees);
        }

        // if request is HATEOUS (by Accept) we must to add links to response
        // on other hand simple shaped data (only Entity without ID)
        private List<Entity> ShapeData(IEnumerable<EmployeeDTO> employeesDTO, string fields) =>
                        _dataShaper.ShapeData(employeesDTO, fields)
                            .Select(x => x.Entity)
                                .ToList();

        // check for response should be HATEOUS-enriched
        private bool ShouldGenerateLinks(HttpContext httpContext)
        {
            var mediaType = (MediaTypeHeaderValue)httpContext.Items["AcceptHeaderMediaType"];

            return mediaType.SubTypeWithoutSuffix.EndsWith("hateoas",
                StringComparison.InvariantCultureIgnoreCase);
        }

        private LinkResponse ReturnedShapedEmployees(List<Entity> shapedEmployees) =>
            new LinkResponse { ShapedEntities = shapedEmployees };

        private LinkResponse ReturnLinkedEmployees(IEnumerable<EmployeeDTO> employeesDTO, 
                                                                string fields, 
                                                                        Guid companyId, 
                                                                            HttpContext httpContext,
                                                                                List<Entity> shapedEmployees)
        {
            var employeesDTOList = employeesDTO.ToList();

            for(var index = 0; index < employeesDTOList.Count(); index++)
            {
                var employeeLinks = CreateLinksForEmployee(httpContext, 
                                                               companyId, 
                                                                    employeesDTOList[index].Id, 
                                                                        fields);
                shapedEmployees[index].Add("Links", employeeLinks);
            }

            var employeeCollection = new LinkCollectionWrapper<Entity>(shapedEmployees);
            var linkedEmployees = CreateLinksForEmployees(httpContext, employeeCollection);

            return new LinkResponse {  HasLinks  = true , LinkedEntities = linkedEmployees };
        }


        private LinkCollectionWrapper<Entity> CreateLinksForEmployees(HttpContext httpContext,
                                                                            LinkCollectionWrapper<Entity> employeesWrapper)
        {
            employeesWrapper.Links.Add(
                new Link(
                    _linkGenerator.GetUriByAction(httpContext
                     , "GetEmployeesForCompany", values: new { })
                     ,  "self"
                     ,  "GET"
                ));

            return employeesWrapper;
        }

        private List<Link> CreateLinksForEmployee(HttpContext httpContext
                                                        , Guid companyId
                                                            , Guid id
                                                                , string fields = "")
        {
            var links = new List<Link>
            {
                new Link(_linkGenerator.GetUriByAction(httpContext, "GetEmployeeForCompany", values: new { companyId, id, fields })
                , "self"
                , "GET"),
                new Link(_linkGenerator.GetUriByAction(httpContext, "DeleteEmployeeForCompany", values: new { companyId, id })
                , "delete_employee"
                ,"DELETE"),
                new Link(_linkGenerator.GetUriByAction(httpContext, "UpdateEmployeeForCompany", values: new { companyId, id })
                , "update_employee"
                , "PUT"),
                new Link(_linkGenerator.GetUriByAction(httpContext, "PartiallyUpdateEmployeeForCompany", values: new { companyId, id })
                , "partially_update_employee"
                , "PATCH")
            };

            return links;
        }

    }
}
