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

            #region Floor
            CreateMap<Floor, FloorResponse>().ReverseMap();
            #endregion

            //#region Customer

            //CreateMap<Customer, CustomerResponse>().ReverseMap();
            //CreateMap<Customer, OrderCustomerResponse>().ReverseMap();
            //CreateMap<Customer, OrderDetailResponse>();
            //CreateMap<CreateCustomerRequest, Customer>();
            //CreateMap<UpdateCustomerRequest, Customer>();

            //#endregion

            //#region Product

            //CreateMap<Product, ProductResponse>()
            //            .IncludeMembers(x => x.Store, x => x.Category).ReverseMap();
            //CreateMap<Product, ProductInMenuResponse>()
            //            .ForMember(x => x.Id, otp => otp.Ignore())
            //            .ReverseMap();

            //CreateMap<CreateProductRequest, Product>();
            //CreateMap<UpdateProductRequest, Product>();

            //CreateMap<UpdateProductExtraRequest, CreateExtraProductRequest>();
            //CreateMap<UpdateProductExtraRequest, Product>()
            //            .ForMember(c => c.Id, option => option.Ignore());

            //CreateMap<Store, ProductResponse>();

            //#endregion

            //#region Order
            //CreateMap<Order, GenOrderResponse>().ReverseMap();
            //CreateMap<Order, OrderResponse>()
            //    .ForMember(x => x.StoreName, opt => opt.MapFrom(y => y.Store.StoreName))
            //    .ReverseMap();
            //CreateMap<Order, OrderDetailResponse>();

            //CreateMap<CreatePreOrderRequest, GenOrderResponse>();
            //CreateMap<CreatePreOrderRequest, OrderResponse>();
            //CreateMap<ListDetailByStore, OrderResponse>().ReverseMap();
            //CreateMap<CreatePreOrderRequest, Order>();

            //CreateMap<CreateOrderRequest, Order>();

            //#endregion

            //#region OrderDetail

            //CreateMap<CreatePreOrderDetailRequest, OrderDetail>();
            //CreateMap<CreatePreOrderDetailRequest, OrderDetailResponse>();

            //CreateMap<CreateOrderDetailRequest, OrderDetail>();

            //CreateMap<PreOrderDetailRequest, OrderDetailResponse>();
            //CreateMap<ProductInMenu, PreOrderDetailRequest>()
            //            .IncludeMembers(x => x.Product)
            //            .ReverseMap();
            //CreateMap<Product, PreOrderDetailRequest>();
            //#endregion

            //#region Store

            //CreateMap<Store, StoreResponse>()
            //    .IncludeMembers(x => x.Destination)
            //    .ReverseMap();
            //CreateMap<StoreResponse, Destination>().ReverseMap();
            //CreateMap<CreateStoreRequest, Store>();
            //CreateMap<UpdateStoreRequest, Store>();

            //#endregion

            //#region Staff

            //CreateMap<Staff, StaffResponse>().ReverseMap();
            //CreateMap<CreateStaffRequest, Staff>();
            //CreateMap<UpdateStaffRequest, Staff>();

            //#endregion

            //#region Menu

            //CreateMap<Menu, MenuResponse>()
            //    .ForMember(x => x.Products, map => map.MapFrom(menu => menu.ProductInMenus))
            //    .ReverseMap();
            //CreateMap<CreateMenuRequest, Menu>();
            //CreateMap<UpdateMenuRequest, Menu>();
            //CreateMap<Menu, ProductResponse>();
            //CreateMap<Menu, ProductInMenu>().ReverseMap();
            //CreateMap<Menu, MenuWithoutProductResponse>();
            //#endregion

            //#region TimeSlot

            //CreateMap<TimeSlot, TimeslotResponse>().ReverseMap();
            //CreateMap<TimeSlot, OrderTimeSlotResponse>().ReverseMap();
            //CreateMap<CreateTimeslotRequest, TimeSlot>()
            //    .ForMember(x => x.ArriveTime, opt => opt.Ignore())
            //    .ForMember(x => x.CheckoutTime, opt => opt.Ignore());
            //CreateMap<UpdateTimeslotRequest, TimeSlot>()
            //    .ForMember(x => x.ArriveTime, opt => opt.Ignore())
            //    .ForMember(x => x.CheckoutTime, opt => opt.Ignore());

            //#endregion

            //#region Product In Menu
            //CreateMap<ProductInMenu, ProductInMenuResponse>()
            //            .IncludeMembers(x => x.Product, x => x.Product.Store, x => x.Product.Category)
            //            .ReverseMap();
            //CreateMap<ProductInMenu, Product>().ReverseMap();

            //CreateMap<AddProductToMenuRequest, ProductInMenu>();
            //CreateMap<UpdateProductInMenuRequest, ProductInMenu>();
            //#endregion

            //CreateMap<Store, ProductInMenuResponse>().ReverseMap();
            //CreateMap<IGrouping<Menu, ProductInMenu>, MenuResponse>().ReverseMap();
        }
    }
}