using AutoMapper;
using FINE.Data.Entity;
using FINE.Service.DTO.Request.Area;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Request.Customer;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Request.Staff;
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
            CreateMap<Campus, CampusResponse>().ReverseMap();
            CreateMap<CreateCampusRequest, Campus>();
            CreateMap<UpdateCampusRequest, Campus>();
            #endregion

            #region Product
            CreateMap<Product, ProductResponse>().ReverseMap();
            CreateMap<CreateProductRequest, Product>();
            CreateMap<UpdateProductRequest, Product>();
            CreateMap<UpdateProductExtraRequest, CreateExtraProductRequest>();
            CreateMap<UpdateProductExtraRequest, Product>()
                .ForMember(c => c.Id, option => option.Ignore());
            #endregion

            #region Area
            CreateMap<Area, AreaResponse>().ReverseMap();
            CreateMap<CreateAreaRequest, Area>();
            CreateMap<UpdateAreaRequest, Area>();
            #endregion

            #region Staff
            CreateMap<Staff, StaffResponse>().ReverseMap();
            CreateMap<CreateStaffRequest, Staff>();
            CreateMap<UpdateStaffRequest, Staff>();
            #endregion
        }
    }
}