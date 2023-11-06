using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Destination;
using FINE.Service.DTO.Request.Menu;
using FINE.Service.DTO.Request.Station;
using FINE.Service.DTO.Request.Store;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;


namespace FINE.Service.Service
{
    public interface IMenuService
    {
        Task<BaseResponseViewModel<MenuResponse>> GetMenuById(string menuId);
        Task<BaseResponseViewModel<MenuByTimeSlotResponse>> GetMenuByTimeslot(string customerId, string timeslotId);
        Task<BaseResponsePagingViewModel<MenuResponse>> GetMenuByTimeslotForAdmin(string timeslotId, PagingRequest paging);
        Task<BaseResponseViewModel<MenuResponse>> CreateMenu(CreateMenuRequest request);
        Task<BaseResponseViewModel<MenuResponse>> UpdateMenu(string menuId, UpdateMenuRequest request);
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
                var result = new MenuByTimeSlotResponse()
                {
                    Menus = new List<MenuResponse>(),
                    ReOrders = new List<ReOrderResponse>()
                };
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
                var reOrders = _unitOfWork.Repository<Order>().GetAll()
                                .Where(x => x.CustomerId == Guid.Parse(customerId)
                                    && x.TimeSlotId == Guid.Parse(timeslotId)
                                    && x.OrderStatus == (int)OrderStatusEnum.Finished)
                                .ToList();
                foreach(var reOrder in reOrders)
                {
                    result.ReOrders.Add(new ReOrderResponse
                    {
                        Id = reOrder.Id,
                        CheckInDate = reOrder.CheckInDate,
                        ItemQuantity = reOrder.ItemQuantity,
                        StationName = listStation.FirstOrDefault(x => x.Id == reOrder.StationId).Name,
                        ListProductInReOrder = _mapper.Map<List<ProductInReOrder>>(reOrder.OrderDetails)
                    });
                }

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

        public async Task<BaseResponsePagingViewModel<MenuResponse>> GetMenuByTimeslotForAdmin(string timeslotId, PagingRequest paging)
        {
            try
            {
                var checkTimeslot = _unitOfWork.Repository<TimeSlot>().GetAll().FirstOrDefault(x => x.Id == Guid.Parse(timeslotId));
                if (checkTimeslot == null)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                       TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());

                var menus = _unitOfWork.Repository<Menu>().GetAll()
                            .Where(x => x.TimeSlotId == Guid.Parse(timeslotId))
                            .OrderByDescending(x => x.CreateAt)
                            .ProjectTo<MenuResponse>(_mapper.ConfigurationProvider)
                            .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<MenuResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = menus.Item1
                    },
                    Data = menus.Item2.ToList()
                };
            }
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<MenuResponse>> CreateMenu(CreateMenuRequest request)
        {
            try
            {
                var checkTimeslot = _unitOfWork.Repository<TimeSlot>().GetAll().FirstOrDefault(x => x.Id == request.TimeSlotId);
                if (checkTimeslot == null)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                       TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());

                var menu = _mapper.Map<CreateMenuRequest, Menu>(request);

                menu.Id = Guid.NewGuid();
                menu.IsActive = true;
                menu.CreateAt = DateTime.Now;

                await _unitOfWork.Repository<Menu>().InsertAsync(menu);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<MenuResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<MenuResponse>(menu)
                };
            }
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<MenuResponse>> UpdateMenu(string menuId, UpdateMenuRequest request)
        {
            try
            {
                var menu = _unitOfWork.Repository<Menu>().GetAll().FirstOrDefault(x => x.Id == Guid.Parse(menuId));
                if (menu == null)
                    throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND,
                       MenuErrorEnums.NOT_FOUND.GetDisplayName());

                var checkTimeslot = _unitOfWork.Repository<TimeSlot>().GetAll().FirstOrDefault(x => x.Id == request.TimeSlotId);
                if (checkTimeslot == null)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                       TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());

                var updateMenu = _mapper.Map<UpdateMenuRequest, Menu>(request, menu);

                updateMenu.UpdateAt = DateTime.Now;

                await _unitOfWork.Repository<Menu>().UpdateDetached(updateMenu);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<MenuResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<MenuResponse>(updateMenu)
                };
            }
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
