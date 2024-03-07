using MediatR;
using Shared.DTO;

namespace Application.Companies.Commands
{
    public sealed record CreateCompanyCommand(CompanyCreateDTO Company) : IRequest<CompanyDTO>;
}
