using Application.Companies.Commands;
using Application.Companies.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DTO;
using System.Runtime.CompilerServices;

namespace CompanyEmployees.Presentation.Controllers
{
    [ApiVersion("3.0")]
    [Route("api/companies")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v3")]
    public class CompaniesController_v3 : ControllerBase
    {
        private readonly ISender _sender;
        public CompaniesController_v3(ISender sender) => _sender = sender;

        [HttpGet]
        public async Task<IEnumerable<CompanyDTO>> GetCompanies()
        {
            var companiesDTO = await _sender.Send(new GetCompaniesQuery(trackChages: false));
            return companiesDTO;
        }

        [HttpGet("{id:guid}", Name ="CompanyId")]
        public async Task<IActionResult> GetCompany(Guid Id)
        {
            var company = await _sender.Send(new GetCompanyQuery(Id: Id, trackChanges: false));
            return Ok(company);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompany([FromBody] CompanyCreateDTO companyForCreateDTO)
        {
            if (companyForCreateDTO is null)
                return BadRequest("CompanyCreateDTO object is null");

            var company = await _sender.Send(new CreateCompanyCommand(companyForCreateDTO));
            return CreatedAtRoute("CompanyId", new { id = company.Id }, company);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCompany(Guid Id, CompanyForUpdateDTO companyForUpdateDTO)
        {
            if (companyForUpdateDTO is null)
                return BadRequest("CompanyForUpdateDTO object is null");

            await _sender.Send(new UpdateCompanyCommand(Id, companyForUpdateDTO, TrackChanges: true));

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCompany(Guid Id)
        {
            await _sender.Send(new DeleteCompanyCommand(Id, TrackChanges: false));

            return NoContent();
        }
    }
}
