using AutoMapper;
using FINE.Data.Entity;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Request.Customer;
using FINE.Service.DTO.Response;

namespace FINE.API.Mapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            #region Customer
            CreateMap<Customer, CustomerResponse>().ReverseMap();
            //CreateMap<Customer, OrderCustomerResponse>().ReverseMap();
            //CreateMap<Customer, OrderCustomerResponse>().ReverseMap();
            CreateMap<CreateCustomerRequest, Customer>();
            CreateMap<UpdateCustomerRequest, Customer>();
            #endregion

            #region Campus
            //CreateMap<Campus, CampusResponse>().ReverseMap();
            CreateMap<CreateCampusRequest, Campus>();
            CreateMap<UpdateCampusRequest, Campus>();
            #endregion
        }
    }
}