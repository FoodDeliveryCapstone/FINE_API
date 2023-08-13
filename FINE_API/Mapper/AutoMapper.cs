using AutoMapper;
using FINE.Data.Entity;
using FINE.Service.DTO.Request.Area;
using FINE.Service.DTO.Request.Destination;
using FINE.Service.DTO.Request.Customer;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Request.Staff;
using FINE.Service.DTO.Request.Store;
using FINE.Service.DTO.Response;
using FINE.Service.DTO.Request.Noti;
using FINE.Service.DTO.Request.TimeSlot;
using FINE.Service.DTO.Request.Menu;
using FINE.Service.DTO.Request.ProductInMenu;
using FINE.API.Controllers;

namespace FINE.API.Mapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            #region Destination
            CreateMap<Destination, DestinationResponse>().ReverseMap();
            CreateMap<CreateDestinationRequest, Destination>();
            CreateMap<UpdateDestinationRequest, Destination>();
            #endregion


            #region TimeSlot

            CreateMap<TimeSlot, TimeslotResponse>().ReverseMap();
            //CreateMap<CreateTimeslotRequest, TimeSlot>()
            //    .ForMember(x => x.ArriveTime, opt => opt.Ignore())
            //    .ForMember(x => x.CheckoutTime, opt => opt.Ignore());
            //CreateMap<UpdateTimeslotRequest, TimeSlot>()
            //    .ForMember(x => x.ArriveTime, opt => opt.Ignore())
            //    .ForMember(x => x.CheckoutTime, opt => opt.Ignore());
            #endregion

            #region Floor
            CreateMap<Floor, FloorResponse>().ReverseMap();
            #endregion

            #region Area

            #endregion

            #region Station
            CreateMap<Station, StationResponse>().ReverseMap();
            #endregion

            #region Box

            #endregion

            #region Store
            CreateMap<Store, StoreResponse>().ReverseMap();
            CreateMap<CreateStoreRequest, Store>();
            CreateMap<UpdateStoreRequest, Store>();
            #endregion

            #region Product
            CreateMap<Product, ProductResponse>().ReverseMap();

            CreateMap<CreateProductRequest, Product>();
            CreateMap<UpdateProductRequest, Product>();
            #endregion

            #region Menu
            CreateMap<Menu, MenuResponse>().ReverseMap();
            CreateMap<CreateMenuRequest, Menu>();
            CreateMap<UpdateMenuRequest, Menu>();
            #endregion

            #region Product In Menu
            CreateMap<ProductInMenu, ProductInMenuResponse>().ReverseMap();
            CreateMap<ProductAttribute, ProductInMenuResponse>().ReverseMap();

            //CreateMap<AddProductToMenuRequest, ProductInMenu>();
            //CreateMap<UpdateProductInMenuRequest, ProductInMenu>();
            #endregion

            #region Product Attribute
            CreateMap<ProductAttribute, ProductAttributeResponse>().ReverseMap();
            #endregion

            #region Order + OrderDetail
            CreateMap<CreateOrderRequest, Order>();
            CreateMap<CreateOrderDetail, OrderDetail>();
            CreateMap<OrderOtherAmount, OtherAmount>().ReverseMap();

            CreateMap<Order, OrderResponse>().ReverseMap();
            CreateMap<OrderDetail, OrderDetailResponse>().ReverseMap();
            CreateMap<Customer, CustomerOrderResponse>().ReverseMap();
            CreateMap<Customer, CustomerCoOrderResponse>().ReverseMap();
            CreateMap<TimeSlot, TimeSlotOrderResponse>().ReverseMap();
            CreateMap<Station, StationOrderResponse>().ReverseMap();

            CreateMap<Order, OrderForStaffResponse>().ReverseMap();
            #endregion

            #region Customer
            CreateMap<Customer, CustomerResponse>().ReverseMap();
            CreateMap<CreateCustomerRequest, Customer>();
            //CreateMap<UpdateCustomerRequest, Customer>();
            #endregion





            #region Staff

            CreateMap<Staff, StaffResponse>().ReverseMap();
            CreateMap<CreateStaffRequest, Staff>();
            //CreateMap<UpdateStaffRequest, Staff>();

            #endregion

            //CreateMap<Store, ProductInMenuResponse>().ReverseMap();
            //CreateMap<IGrouping<Menu, ProductInMenu>, MenuResponse>().ReverseMap();
        }
    }
}