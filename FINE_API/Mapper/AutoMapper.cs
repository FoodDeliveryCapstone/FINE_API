using AutoMapper;
using FINE.Data.Entity;
using FINE.Service.DTO.Request.Area;
using FINE.Service.DTO.Request.BlogPost;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Request.Customer;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Request.Staff;
using FINE.Service.DTO.Request.StaffReport;
using FINE.Service.DTO.Request.ProductCollection;
using FINE.Service.DTO.Request.Product_Collection_Time_Slot;
using FINE.Service.DTO.Request.Store;
using FINE.Service.DTO.Request.UniversityInfo;
using FINE.Service.DTO.Request.Product_Collection_Item;
using FINE.Service.DTO.Request.Store_Category;
using FINE.Service.DTO.Request.SystemCategory;
using FINE.Service.DTO.Request.University;
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
            #region Customer

            CreateMap<Customer, CustomerResponse>().ReverseMap();
            CreateMap<Customer, OrderCustomerResponse>().ReverseMap();
            CreateMap<Customer, OrderCustomerResponse>().ReverseMap();
            CreateMap<CreateCustomerRequest, Customer>();
            CreateMap<UpdateCustomerRequest, Customer>();

            #endregion

            #region System Category

            CreateMap<SystemCategory, SystemCategoryResponse>().ReverseMap();
            CreateMap<CreateSystemCategoryRequest, SystemCategory>();
            CreateMap<UpdateSystemCategoryRequest, SystemCategory>();

            #endregion

            #region Store Category

            CreateMap<StoreCategory, StoreCategoryResponse>().ReverseMap();
            CreateMap<CreateStoreCategoryRequest, StoreCategory>();
            CreateMap<CreateStoreCategoryRequest, StoreCategory>();

            #endregion

            #region Product

            CreateMap<Product, ProductResponse>().IncludeMembers(x => x.Store, x => x.Category).ReverseMap();
            CreateMap<Store, ProductResponse>();
            CreateMap<Product, ProductInMenuResponse>()
                .ForMember(x => x.Id ,otp => otp.Ignore())
                .ReverseMap();
            CreateMap<SystemCategory, ProductResponse>();
            CreateMap<CreateProductRequest, Product>();
            CreateMap<UpdateProductRequest, Product>();
            CreateMap<UpdateProductExtraRequest, CreateExtraProductRequest>();
            CreateMap<UpdateProductExtraRequest, Product>()
                .ForMember(c => c.Id, option => option.Ignore());

            #endregion

            #region Product Collection

            CreateMap<ProductCollection, ProductCollectionResponse>().ReverseMap();
            CreateMap<CreateProductCollectionRequest, ProductCollection>();
            CreateMap<UpdateProductCollectionRequest, ProductCollection>();

            #endregion

            #region Order

            CreateMap<Order, OrderResponse>().ReverseMap();
            CreateMap<Order, GenOrderResponse>().ReverseMap();
            CreateMap<CreatePreOrderRequest, GenOrderResponse>().ReverseMap();
            CreateMap<CreatePreOrderRequest, OrderResponse>().ReverseMap();
            CreateMap<ListDetailByStore, OrderResponse>().ReverseMap();
            CreateMap<CreatePreOrderRequest, Order>();
            CreateMap<CreateGenOrderRequest, Order>();
            CreateMap<CreateOrderRequest, Order>();

            #endregion

            #region OrderDetail

            CreateMap<OrderDetail, OrderDetailResponse>()
               .IncludeMembers(x => x.Order, x => x.Order.Customer)
               .ReverseMap();
            CreateMap<CreatePreOrderDetailRequest, OrderDetail>();
            CreateMap<CreatePreOrderDetailRequest, OrderDetailResponse>();
            CreateMap<PreOrderDetailRequest, OrderDetailResponse>();
            CreateMap<CreateOrderDetailRequest, OrderDetail>();
            CreateMap<Order, OrderDetailResponse>();
            CreateMap<Customer, OrderDetailResponse>();
            #endregion

            #region Store

            CreateMap<Store, StoreResponse>().ReverseMap();
            CreateMap<CreateStoreRequest, Store>();
            CreateMap<UpdateStoreRequest, Store>();

            #endregion

            #region Area

            CreateMap<Area, AreaResponse>().ReverseMap();
            CreateMap<CreateAreaRequest, Area>();
            CreateMap<UpdateAreaRequest, Area>();

            #endregion

            #region Room

            CreateMap<Room, RoomResponse>().ReverseMap();
            CreateMap<Room, OrderRoomResponse>()
                .ForMember(orderRoom => orderRoom.AreaName, map => map.MapFrom(room => room.Area.Name)).ReverseMap();
            #endregion

            #region Campus

            CreateMap<Campus, CampusResponse>().ReverseMap();
            CreateMap<CreateCampusRequest, Campus>();
            CreateMap<UpdateCampusRequest, Campus>();

            #endregion

            #region Staff

            CreateMap<Staff, StaffResponse>().ReverseMap();
            CreateMap<CreateStaffRequest, Staff>();
            CreateMap<UpdateStaffRequest, Staff>();

            #endregion

            #region BlogPost

            CreateMap<BlogPost, BlogPostResponse>().ReverseMap();
            CreateMap<CreateBlogPostRequest, BlogPost>();
            CreateMap<UpdateBlogPostRequest, BlogPost>();

            #endregion

            #region University

            CreateMap<University, UniversityResponse>().ReverseMap();
            CreateMap<CreateUniversityRequest, University>();
            CreateMap<UpdateUniversityRequest, University>();

            #endregion

            #region Menu

            CreateMap<Menu, MenuResponse>().ForMember(menuResponse => menuResponse.Products, map => map.MapFrom(menu => menu.ProductInMenus)).ReverseMap();
            CreateMap<CreateMenuRequest, Menu>();
            CreateMap<UpdateMenuRequest, Menu>();           
            CreateMap<Menu, ProductResponse>();
            CreateMap<Menu, ProductInMenu>().ReverseMap();
            #endregion

            #region TimeSlot

            CreateMap<TimeSlot, TimeslotResponse>().ReverseMap();
            CreateMap<TimeSlot, OrderTimeSlotResponse>().ReverseMap();
            CreateMap<CreateTimeslotRequest, TimeSlot>();
            CreateMap<UpdateTimeslotRequest, TimeSlot>();

            //#region Get Product By Timeslot
            //CreateMap<TimeSlot, TimeslotResponse>()
            //    .ForMember(dest => dest.prodctCollectionTimeSlots, opt => opt.MapFrom(src => src.ProductCollectionTimeSlots)).ReverseMap();

            //CreateMap<ProductCollectionTimeSlot, ProductCollectionTimeSlotResponse>()
            //    .ForMember(dest => dest.productCollection, opt => opt.MapFrom(src => src.ProductCollection)).ReverseMap();

            //CreateMap<ProductCollection, ProductCollectionResponse>()
            //    .ForMember(dest => dest.productionItemCollections, opt => opt.MapFrom(src => src.ProductionCollectionItems)).ReverseMap();
            
            //CreateMap<ProductionCollectionItem, ProductionCollectionItemResponse>()
            //    .ForMember(dest => dest.product, opt => opt.MapFrom(src => src.Product)).ReverseMap();
            
            //CreateMap<Product, ProductResponse>()
            //    .ForMember(dest => dest.products, opt => opt.MapFrom(src => src.InverseGeneralProduct)).ReverseMap();
            //#endregion
            #endregion

            #region Product In Menu
            CreateMap<ProductInMenu, AddProductToMenuResponse>().ReverseMap();
            CreateMap<AddProductToMenuRequest, ProductInMenu>();
            CreateMap<UpdateProductInMenuRequest, ProductInMenu>();
            //CreateMap<ProductInMenu, ProductResponse>()/*.ReverseMap()*/;
            CreateMap<ProductInMenu, Product>().ReverseMap();
            CreateMap<Store, ProductInMenuResponse>().ReverseMap();
            CreateMap<SystemCategory, ProductInMenuResponse>().ReverseMap();
            CreateMap<ProductInMenu, ProductInMenuResponse>()
                .IncludeMembers(x => x.Product, x => x.Product.Store, x => x.Product.Category)
                .ReverseMap();
            #endregion

            #region
            CreateMap<Floor, FloorResponse>().ReverseMap();
            #endregion
        }
    }
}