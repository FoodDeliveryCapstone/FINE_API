using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Destination;
using FINE.Service.DTO.Request.Menu;
using FINE.Service.DTO.Request.Store;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;


namespace FINE.Service.Service
{
    public interface IMenuService
    {
        Task<BaseResponseViewModel<MenuResponse>> GetMenuById(string menuId);
        Task<BaseResponseViewModel<MenuByTimeSlotResponse>> GetMenuByTimeslot(string customerId, string timeslotId);
    }

    public class MenuService : IMenuService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public MenuService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<MenuResponse>> GetMenuById(string menuId)
        {
            try
            {
                var menu = _unitOfWork.Repository<Menu>().GetAll()
                                    .Where(x => x.Id == Guid.Parse(menuId))
                                    .ProjectTo<MenuResponse>(_mapper.ConfigurationProvider)
                                    .FirstOrDefault();

                if (menu == null)
                    throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND,
                                        MenuErrorEnums.NOT_FOUND.GetDisplayName());

                menu.Products = _unitOfWork.Repository<ProductInMenu>().GetAll()
                                            .Include(x => x.Product)
                                            .ThenInclude(x => x.Product)
                                            .Where(x => x.MenuId == menu.Id
                                                     && x.Status == (int)ProductInMenuStatusEnum.Avaliable)
                                            .GroupBy(x => x.Product.Product)
                                            .Select(x => _mapper.Map<ProductResponse>(x.Key))
                                            .ToList();

                return new BaseResponseViewModel<MenuResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = menu
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<MenuByTimeSlotResponse>> GetMenuByTimeslot(string customerId, string timeslotId)
        {
            try
            {
                var result = new MenuByTimeSlotResponse();
                var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                                            .FirstOrDefault(x => x.Id == Guid.Parse(timeslotId));

                if (timeslot == null)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                        TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());

                result.Menus = _unitOfWork.Repository<Menu>().GetAll()
                                    .Where(x => x.TimeSlotId == Guid.Parse(timeslotId) && x.IsActive == true)
                                    .ProjectTo<MenuResponse>(_mapper.ConfigurationProvider)
                                    .ToList();
                foreach(var menu in result.Menus)
                {
                    menu.Products = _unitOfWork.Repository<ProductInMenu>().GetAll()
                                            .Include(x => x.Product)
                                            .ThenInclude(x => x.Product)
                                            .Where(x => x.MenuId == menu.Id 
                                            && x.IsActive == true
                                            && x.Status == (int)ProductInMenuStatusEnum.Avaliable)
                                            .GroupBy(x => x.Product.Product)
                                            .Select(x => _mapper.Map<ProductResponse>(x.Key))
                                            .ToList();
                }
                var listStation = _unitOfWork.Repository<Station>().GetAll().Where(x => x.Floor.DestionationId == timeslot.DestinationId).ToList();
                result.ReOrders = _unitOfWork.Repository<Order>().GetAll()
                                .Where(x => x.CustomerId == Guid.Parse(customerId)
                                    && x.TimeSlotId == Guid.Parse(timeslotId)
                                    && x.OrderStatus == (int)OrderStatusEnum.Finished)
                                .Select(x => new ReOrderResponse
                                {
                                    Id = x.Id,
                                    CheckInDate = x.CheckInDate,
                                    ItemQuantity = x.ItemQuantity,
                                    StationName = listStation.FirstOrDefault(z => z.Id == x.StationId).Name,
                                    ListProductInReOrder = _mapper.Map<List<ProductInReOrder>>(x.OrderDetails)
                                }).ToList();

                return new BaseResponseViewModel<MenuByTimeSlotResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = result
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
