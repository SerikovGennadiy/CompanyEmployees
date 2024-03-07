using Application.Companies.Commands;
using Application.Notifications;
using Contracts;
using Entities.Excepions;
using MediatR;

namespace Application.Companies.Handlers
{
    //internal sealed class DeleteCompanyHandler : IRequestHandler<DeleteCompanyCommand, Unit>
    internal sealed class DeleteCompanyHandler : INotificationHandler<CompanyDeletedNotification>
    {
        private readonly IRepositoryManager _repository;

        public DeleteCompanyHandler(IRepositoryManager repository)
        {
            _repository = repository;
        }

        //public async Task<Unit> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
        public async Task Handle(CompanyDeletedNotification notification, CancellationToken cancellationToken)
        {
              // var company = await _repository.Company.GetCompanyByIdAsync(request.Id, request.TrackChanges);
            var company = await _repository.Company.GetCompanyByIdAsync(notification.Id, notification.TrackChanges);

            if (company is null)
              // throw new CompanyNotFoundException(request.Id);
                    throw new CompanyNotFoundException(notification.Id);

            _repository.Company.DeleteCompany(company);

            await _repository.SaveAsync();
             // return Unit.Value;
        }
    }
}
