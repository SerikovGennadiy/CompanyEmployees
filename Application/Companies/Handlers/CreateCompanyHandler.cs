using Application.Companies.Commands;
using AutoMapper;
using Contracts;
using Entities.Models;
using MediatR;
using Shared.DTO;

namespace Application.Companies.Handlers
{
   internal sealed class CreateCompanyHandler : IRequestHandler<CreateCompanyCommand, CompanyDTO>
    {
        private readonly IRepositoryManager _repository;
        private readonly IMapper _mapper;

        public CreateCompanyHandler(IRepositoryManager repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<CompanyDTO> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
        {
            var companyEntity = _mapper.Map<Company>(request.Company);
            _repository.Company.CreateCompany(companyEntity);
            await _repository.SaveAsync();

            var companyToReturn = _mapper.Map<CompanyDTO>(companyEntity);

            return companyToReturn;
        }

    }
}
