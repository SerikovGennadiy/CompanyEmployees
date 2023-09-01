using CompanyEmployees.Presentaion.ActionFilters;
using CompanyEmployees.Presentation.ActionFilters;
using Entities.LinkModels;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Service.Contracts;
using Shared.DTO;
using Shared.RequestFeatures;
using System.Text.Json;

namespace CompanyEmployees.Presentation.Controllers
{
    // because employee is child entity for company
    // access to employee only through Company root route!!!
    [Route("api/companies/{companyId}/employees")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly IServiceManager _service;
        public EmployeesController(IServiceManager service) => _service = service;

        //------------------- skip / take refactoring -------------------
        //[HttpGet] // companyId mapped from main route (look up)
        //public async Task<IActionResult> GetEmployeesForCompany(Guid companyId,
        //    [FromQuery] EmployeeParameters employeeParameters)
        //{
        //    var employees = await _service.EmployeeService.GetEmployeesAsync(companyId, employeeParameters, trackChanges: false);
        //    return Ok(employees);
        //}

        // DATA SHAPING
        //[HttpGet]
        //[ServiceFilter(typeof(ValidateMediaTypeAttribute))]
        //public async Task<IActionResult> GetEmployeesForCompany(Guid companyId,
        //    [FromQuery] EmployeeParameters employeeParameters)
        //{
        //    var pagedResult = await _service.EmployeeService.GetEmployeesAsync(companyId, employeeParameters, trackChages: false);

        //    // all metadata will go to X-Pagination header
        //    // this information is very usefull when we creating any frontend pagination in our benefit (в наших интересах)
        //    // E.g. we can create using this metadata links to next or previous pagination page! but it's in HATEOUS scope
        //    Response.Headers.Add("X-Pagination",
        //        JsonSerializer.Serialize(pagedResult.metaData));

        //    return Ok(pagedResult.employees);
        //}

        // HATEOUS
        [HttpGet]
        [HttpHead]
        [ServiceFilter(typeof(ValidateMediaTypeAttribute))]
        public async Task<IActionResult> GetEmployeesForCompany(Guid companyId,
            [FromQuery] EmployeeParameters employeeParameters)
        {
            // HATEOUS settings
            var linkParams = new LinkParameters(employeeParameters, HttpContext);
            var result = await _service.EmployeeService.GetEmployeesAsync(companyId, linkParams, trackChanges: false);

            // all metadata will go to X-Pagination header
            // this information is very usefull when we creating any frontend pagination in our benefit (в наших интересах)
            // E.g. we can create using this metadata links to next or previous pagination page! but it's in HATEOUS scope
            Response.Headers.Add("X-Pagination",
                JsonSerializer.Serialize(result.metaData));

            return result.linkResponse.HasLinks ?
                Ok(result.linkResponse.LinkedEntities) :
                Ok(result.linkResponse.ShapedEntities);
        }
        //----------------------------------------------------------------

        [HttpGet("{id:guid}", Name = "GetEmployeeForCompany")]
        public async Task<IActionResult> GetEmployeeForCompany(Guid companyId, Guid id,
            [FromQuery] EmployeeParameters employeeParameters)
        {
            var employee = await _service.EmployeeService.GetEmployeeAsync(companyId, id, trackChanges: false);
            return Ok(employee);
        }


        [HttpPost]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> CreateEmployeeForCompany(Guid companyId, [FromBody]EmployeeForCreationDTO employee)
        {
            //if (employee is null)
            //    return BadRequest("EmployeeForCreation dto object is null");

            //if (!ModelState.IsValid)
            //    return UnprocessableEntity(ModelState);

            var employeeForReturn = await _service.EmployeeService.CreateEmployeeForCompanyAsync(companyId, employee, trackChanges: false);
            // create action answer and populate Location header
            // routeValues names MUST BE THE SAME like in passing action!!
            return CreatedAtAction(actionName: "GetEmployeeForCompany",
                                       routeValues: new { companyId, id = employeeForReturn.Id },
                                               value: employeeForReturn);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteEmployeeForCompany(Guid companyId, Guid id)
        {
            await _service.EmployeeService.DeleteEmployeeForCompanyAsync(companyId, id, trackChanges: false);
            return NoContent();
        }

        [HttpPut("{id:guid}")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> UpdateEmployeeForCompany(Guid companyId, Guid id, [FromBody] EmployeeForUpdateDTO employee)
        {
            //if (employee is null)
            //    return BadRequest("EmployeeForUpdateDTO object is null");

            //if (!ModelState.IsValid)
            //    return UnprocessableEntity(ModelState);

            await _service.EmployeeService.UpdateEmployeeForCompanyAsync(companyId, id, employee,
                compTrackChanges: false, empTrackChanges: true);

            return NoContent();
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> PartiallyUpdateEmployeeForCompany(Guid companyId, 
                            Guid id, [FromBody] JsonPatchDocument<EmployeeForUpdateDTO> patchDoc)
        {
            if (patchDoc is null)
                return BadRequest("patchDoc object sent from client is null");

            var result = await _service.EmployeeService.GetEmployeeForPatchAsync(companyId, 
                                    id, compTrackChanges: false, empTrackChanges: true);

            patchDoc.ApplyTo(result.employeeToPatch, ModelState);
            // to prevent incorrect assignment value in validated object
            TryValidateModel(result.employeeToPatch);

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            await _service.EmployeeService.SaveChangesForPatchAsync(result.employeeToPatch, result.employeeEntity);
            return NoContent();
        }
    }
}
