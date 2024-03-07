using AutoMapper;
using Entities.Models;
using Shared.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application
{
    public class MappingProfile : Profile
    {
        public MappingProfile() {
            CreateMap<Company, CompanyDTO>()
                .ForMember(c => c.FullAddress,
                           //.ForCtorParam("FullAddress",
                           opt => opt.MapFrom(x => string.Join(' ', x.Address, x.Country)));

            CreateMap<CompanyCreateDTO, Company>();
        }
    }
}
