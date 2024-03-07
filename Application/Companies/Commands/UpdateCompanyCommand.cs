using MediatR;
using Shared.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Companies.Commands
{
    public sealed record UpdateCompanyCommand(Guid Id, CompanyForUpdateDTO Company, bool TrackChanges) : IRequest;
}
