using Microsoft.AspNetCore.Mvc;
using Service.Contracts;

namespace CompanyEmployees.Presentation.Controllers
{
    [ApiVersion("2.0", Deprecated = true)]
    // for this version (2.0) with API URL Version (look at [Route..] WE CAN'T USE ..api/companies?api-version=2.0
    // BUT for version 1.0 we can  ...api/companies?api-version=1.0 with [Route("api/companies")]
    //[Route("api/{v:apiversion}/companies")] 

    //for Header Verioning comment up [Route...
    [Route("api/companies")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v2")]
    public class CompanyController_v2 : ControllerBase
    {
        private readonly IServiceManager _service;
        public CompanyController_v2(IServiceManager service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _service.CompanyService.GetAllCompaniesAsync(trackChanges: false);

            var copmaniesV2 = companies.Select(x => $"{x.Name} V2");
            return Ok(copmaniesV2);
        }
    }
}
