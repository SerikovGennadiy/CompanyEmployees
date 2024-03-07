using Application.Companies.Commands;
using AutoMapper;
using Contracts;
using Entities.Excepions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Companies.Handlers
{
    // our handler not return any value -> using structure Unit represents void type!!!
    internal sealed class UpdateCompanyHandler : IRequestHandler<UpdateCompanyCommand, Unit>
    {
        private readonly IRepositoryManager _repository;
        private readonly IMapper _mapper;

        public UpdateCompanyHandler(IRepositoryManager repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<Unit> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
        {
            var companyEntity = await _repository.Company.GetCompanyByIdAsync(request.Id, request.TrackChanges);

            if (companyEntity is null)
                throw new CompanyNotFoundException(request.Id);

            _mapper.Map(request.Company, companyEntity);

            await _repository.SaveAsync();

            return Unit.Value;
        }
    }
}
