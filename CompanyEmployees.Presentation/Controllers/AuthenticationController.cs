using CompanyEmployees.Presentaion.ActionFilters;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DTO;

namespace CompanyEmployees.Presentation.Controllers
{
    [Route("api/authentication")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IServiceManager _service;
        public AuthenticationController(IServiceManager service) => _service = service;

        [HttpPost]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<ActionResult> RegisterUser([FromBody] UserForRegistractionDTO userForRegistrationDTO)
        {
            var result = await
                  _service.AuthentificationService.RegisterUser(userForRegistrationDTO);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.TryAddModelError(error.Code, error.Description);
                }
                return BadRequest(ModelState);
            }

            return StatusCode(201);
        }

        [HttpPost("login")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> Authenticate([FromBody] UserForAuthenticationDTO userForAuth)
        {
            if (!await _service.AuthentificationService.ValidateUser(userForAuth))
                return Unauthorized();

            var tokenDTO = await _service.AuthentificationService.CreateToken(populateExp: true);

            /*          return Ok(
                          new { 
                              Token = await _service.AuthentificationService.CreateToken() 
                          });*/
            return Ok(tokenDTO);
        }
    }
}
