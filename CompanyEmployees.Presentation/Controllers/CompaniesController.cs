using CompanyEmployees.Presentaion.ActionFilters;
using CompanyEmployees.Presentation.ModelBinders;
using Marvin.Cache.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DTO;

namespace CompanyEmployees.Presentation.Controllers
{
    #region ApiController attribute
    /*
     * [ApiController] attribute activate following API behavior
     * - Attribute routing requirements
     * - Authomatice HTTP 400 response
     * - Binding source parameter inference(англ. вывод)
     * - Multipart/form-data request inference
     * - Problem details for error status code
     */
    #endregion
    // all controller give access for authoroze users
    [Authorize]
    [Route("api/companies")]
    [ApiController]
    [ApiVersion("1.0")]
    [ApiExplorerSettings(GroupName = "v1")]
   // [ResponseCache(CacheProfileName = "120SecondsDuration")]
    public class CompaniesController : ControllerBase
    {
        private readonly IServiceManager _service;
        public CompaniesController(IServiceManager service) => _service = service;

        /// <summary>
        /// Get the list of all Companies
        /// </summary>
        /// <returns>The companies list</returns>
        [Authorize(Roles="Manager")]
        [HttpGet(Name ="GetCompanies")]
        public async Task<IActionResult> GetCompanies()
        {
            //try
            //{
            //throw new Exception("CEP Exception");
                var companies = await _service.CompanyService.GetAllCompaniesAsync(trackChanges: false);
                return Ok(companies);
            //}
            //catch
            //{
            //    return StatusCode(500, "Internal server error");
            //}
        }

        [HttpGet("{id:guid}", Name = "CompanyById")]
        // action cache settings overrides controller and global settings
        [HttpCacheExpiration(CacheLocation = CacheLocation.Public, MaxAge = 50)]
        [HttpCacheValidation(MustRevalidate = true)]
        //[ResponseCache(Duration = 60)]
        public async Task<IActionResult> GetCompany(Guid id)
        {
            var company = await _service.CompanyService.GetCompanyByIdAsync(id, trackChanges: false);
            return Ok(company);
        }

        /// <summary>
        /// Creates a newly created company
        /// </summary>
        /// <param name="company"></param>
        /// <returns>A newly created company</returns>
        /// <response code="201">Returns a newly created item</response>
        /// <response code="400">If is item is null</response>
        /// <response code="422">If the model is invalid</response>
        [HttpPost(Name = "CreateCompany")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> CreateCompany([FromBody] CompanyCreateDTO company)
        {
            // comment due to [ServiceFilter... this try-catch in there.
            // if (company is null)
            //    return BadRequest("CompanyCreateDTO object is null");

            var createdCompany = await _service.CompanyService.CreateCompanyAsync(company);

            return CreatedAtRoute("CompanyById", new { id = createdCompany.Id }, createdCompany);
        }

        [HttpGet("collections/({ids})", Name = "CompanyCollection")]
        public async Task<IActionResult>
            GetCompanyCollection([ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids)
        {
            var companies = await _service.CompanyService.GetByIdsAsync(ids, trackChanges: false);
            return Ok(companies);
        }

        [HttpPost("collection")]
        public async Task<IActionResult> CreateCompanyCollection([FromBody] IEnumerable<CompanyCreateDTO> companies)
        {
            var result = await _service.CompanyService.CreateCompanyCollectionAsync(companies);
            return CreatedAtRoute("CompanyCollection", new { result.ids }, result.companies);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCompany(Guid id)
        {
            await _service.CompanyService.DeleteCompanyAsync(id, trackChanges: false);
            return NoContent();
        }

        [HttpPut("{id:guid}")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> UpdateCompany(Guid id, CompanyForUpdateDTO company)
        {
            // comment due to [ServiceFilter... this try-catch in there.
            // if (company is null)
            //    return BadRequest("CompanyForUpdateDTO object is null");

            await _service.CompanyService.UpdateCompanyAsync(id, company, trackChanges: true);

            return NoContent();
        }

        [HttpOptions]
        public IActionResult GetCompaniesOptions()
        {
            Response.Headers.Add("Allow", "GET, OPTIONS, POST");
            return Ok();
        }
    }
}
