using MediatR;

namespace Application.Companies.Commands
{
    public sealed record DeleteCompanyCommand(Guid Id, bool TrackChanges) : IRequest;
}
