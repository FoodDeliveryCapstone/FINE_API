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
using FINE.Service.DTO.Request.Menu;
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
            CreateMap<CreateOrderRequest, Order>();

            #endregion

            #region OrderDetail

            CreateMap<OrderDetail, OrderDetailResponse>().ReverseMap();
            CreateMap<CreateOrderDetailRequest, OrderDetail>();

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

            #endregion

            #region Campus

            //CreateMap<Campus, CampusResponse>().ReverseMap();
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

            CreateMap<Menu, MenuResponse>().ReverseMap();
            CreateMap<CreateMenuRequest, Menu>();
            CreateMap<UpdateMenuRequest, Menu>();

            #endregion

            #region TimeSlot

            //CreateMap<TimeSlot, TimeSlotResponse>()
            //    .ForMember(dest => dest.stores, opt => opt.MapFrom(src => src.Campus.Stores))
            //    .ForMember(dest => dest.menus, opt => opt.MapFrom(src => src.Menus))

            #region Get Product By Timeslot
            CreateMap<TimeSlot, TimeSlotResponse>();
            CreateMap<ProductCollectionTimeSlot, ProductCollectionTimeSlotResponse>();
            CreateMap<ProductCollection, ProductCollectionResponse>();
            CreateMap<ProductionCollectionItem, ProductionCollectionItemResponse>();
            CreateMap<Product, GeneralProductResponse>();
            CreateMap<Product, ProductResponse>();
            #endregion
            #endregion
        }
    }
}