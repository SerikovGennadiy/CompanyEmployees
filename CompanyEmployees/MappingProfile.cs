using AutoMapper;
using Entities.Models;
using Shared.DTO;

namespace CompanyEmployees
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Company, CompanyDTO>()
                .ForMember(c => c.FullAddress,
                //.ForCtorParam("FullAddress",
                           opt => opt.MapFrom(x => string.Join(' ', x.Address, x.Country)));

            CreateMap<Employee, EmployeeDTO>();

            CreateMap<CompanyCreateDTO, Company>();

            CreateMap<EmployeeForCreationDTO, Employee>();

            CreateMap<EmployeeForUpdateDTO, Employee>().ReverseMap();

            CreateMap<CompanyForUpdateDTO, Company>();

            CreateMap<UserForRegistractionDTO, User>();
        }
    }
}
