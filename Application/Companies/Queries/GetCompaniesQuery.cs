﻿using MediatR;
using Shared.DTO;

namespace Application.Companies.Queries
{
    public sealed record GetCompaniesQuery(bool trackChages) : IRequest<IEnumerable<CompanyDTO>>
    { }
}
