using Application.Companies.Queries;
using AutoMapper;
using Contracts;
using Entities.Excepions;
using MediatR;
using Shared.DTO;

namespace Application.Companies.Handlers
{
    public sealed class GetCompanyHandler: IRequestHandler<GetCompanyQuery, CompanyDTO>
    {
        private readonly IMapper _mapper;
        private readonly IRepositoryManager _repository;

        public GetCompanyHandler(IMapper mapper, IRepositoryManager repository) 
            
        {
            _mapper = mapper;
            _repository = repository;
        }

        public async Task<CompanyDTO> Handle(GetCompanyQuery request, CancellationToken cancellationToken)
        {
            var company = await _repository.Company.GetCompanyByIdAsync(request.Id, request.trackChanges);
            if(company == null)
                throw new CompanyNotFoundException(request.Id);

            var companyDTO = _mapper.Map<CompanyDTO>(company);
            return companyDTO;
        }
    }
}
