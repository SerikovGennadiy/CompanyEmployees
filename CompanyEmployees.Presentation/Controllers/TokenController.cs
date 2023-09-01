using CompanyEmployees.Presentaion.ActionFilters;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DTO;

namespace CompanyEmployees.Presentation.Controllers
{
    [Route("api/token")]
    [ApiController]
    public class TokenController : ControllerBase {
        private readonly IServiceManager _service;
        public TokenController(IServiceManager service) => _service = service;

        [HttpPost("refresh")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> Refresh([FromBody]TokenDTO tokenDTO)
        {
            var tokenDtoToReturn = await
                _service.AuthentificationService.RefreshToken(tokenDTO);
           
            return Ok(tokenDtoToReturn);
        }
    }

}
