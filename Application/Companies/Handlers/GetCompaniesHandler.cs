using Application.Companies.Queries;
using AutoMapper;
using Contracts;
using Entities.Responses;
using MediatR;
using Shared.DTO;

namespace Application.Companies.Handlers
{
   internal sealed class GetCompaniesHandler : IRequestHandler<GetCompaniesQuery, IEnumerable<CompanyDTO>>
    {
        private readonly IMapper _mapper;
        private readonly IRepositoryManager _repository;
        public GetCompaniesHandler(IRepositoryManager repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        // impls IRequestHandler means Handle would be manage GetCompaniesQuery requests, returning list of Companies
        public async Task<IEnumerable<CompanyDTO>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
        {
            var companies = await _repository.Company.GetAllCompaniesAsync(request.trackChages);
            var companiesDTO = _mapper.Map<IEnumerable<CompanyDTO>>(companies);
            return companiesDTO;
        }

    }
}
