using Application.Companies.Commands;
using Contracts;
using Entities.Excepions;
using MediatR;

namespace Application.Companies.Handlers
{
    internal sealed class DeleteCompanyHandler : IRequestHandler<DeleteCompanyCommand, Unit>
    {
        private readonly IRepositoryManager _repository;

        public DeleteCompanyHandler(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public async Task<Unit> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
        {
            var company = await _repository.Company.GetCompanyByIdAsync(request.Id, request.TrackChanges);

            if (company is null)
                throw new CompanyNotFoundException(request.Id);

            _repository.Company.DeleteCompany(company);

            await _repository.SaveAsync();

            return Unit.Value;
        }
    }
}
