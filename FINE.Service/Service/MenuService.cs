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
        //Task<BaseResponsePagingViewModel<MenuWithoutProductResponse>> GetMenus(MenuWithoutProductResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<MenuResponse>> GetMenuById(string menuId);
        Task<BaseResponseViewModel<List<MenuResponse>>> GetMenuByTimeslot(string timeslotId, PagingRequest paging);
        //Task<BaseResponseViewModel<MenuResponse>> CreateMenu(CreateMenuRequest request);
        //Task<BaseResponseViewModel<MenuResponse>> UpdateMenu(int menuId, UpdateMenuRequest request);
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

        //public async Task<BaseResponseViewModel<MenuResponse>> CreateMenu(CreateMenuRequest request)
        //{
        //    try
        //    {
        //        var menu = _mapper.Map<CreateMenuRequest, Menu>(request);

        //        menu.CreateAt = DateTime.Now;

        //        await _unitOfWork.Repository<Menu>().InsertAsync(menu);
        //        await _unitOfWork.CommitAsync();

        //        return new BaseResponseViewModel<MenuResponse>()
        //        {
        //            Status = new StatusViewModel()
        //            {
        //                Message = "Success",
        //                Success = true,
        //                ErrorCode = 0
        //            },
        //            Data = _mapper.Map<MenuResponse>(menu)
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}

        //public async Task<BaseResponsePagingViewModel<MenuWithoutProductResponse>> GetMenus(MenuWithoutProductResponse filter, PagingRequest paging)
        //{
        //    try
        //    {
        //        var menu = _unitOfWork.Repository<Menu>().GetAll()
        //                                .ProjectTo<MenuWithoutProductResponse>(_mapper.ConfigurationProvider)
        //                                .DynamicFilter(filter)
        //                                .DynamicSort(filter)
        //                                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
        //                                Constants.DefaultPaging);

        //        return new BaseResponsePagingViewModel<MenuWithoutProductResponse>()
        //        {
        //            Metadata = new PagingsMetadata()
        //            {
        //                Page = paging.Page,
        //                Size = paging.PageSize,
        //                Total = menu.Item1
        //            },
        //            Data = menu.Item2.ToList()
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}

        //public async Task<BaseResponseViewModel<MenuResponse>> UpdateMenu(int menuId, UpdateMenuRequest request)
        //{
        //    try
        //    {
        //        var menu = _unitOfWork.Repository<Menu>().GetAll()
        //             .FirstOrDefault(x => x.Id == menuId);

        //        if (menu == null)
        //            throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND,
        //                MenuErrorEnums.NOT_FOUND.GetDisplayName());

        //        var updateMenu = _mapper.Map<UpdateMenuRequest, Menu>(request, menu);

        //        var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
        //                               .Where(x => x.MenuId == menuId);
        //        if (request.IsActive == false)
        //        {

        //            foreach (var productInMenuStatus in productInMenu)
        //            {
        //                productInMenuStatus.Status = (int)ProductInMenuStatusEnum.OutOfStock;
        //            }
        //        }
        //        else
        //        {
        //            foreach (var productInMenuStatus in productInMenu)
        //            {
        //                productInMenuStatus.Status = (int)ProductInMenuStatusEnum.Avaliable;
        //            }
        //        }
        //        updateMenu.UpdateAt = DateTime.Now;

        //        await _unitOfWork.Repository<Menu>().UpdateDetached(updateMenu);
        //        await _unitOfWork.CommitAsync();

        //        return new BaseResponseViewModel<MenuResponse>()
        //        {
        //            Status = new StatusViewModel()
        //            {
        //                Message = "Success",
        //                Success = true,
        //                ErrorCode = 0
        //            },
        //            Data = _mapper.Map<MenuResponse>(menu)
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}

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
                                            .Where(x => x.MenuId == menu.Id)
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

        public async Task<BaseResponseViewModel<List<MenuResponse>>> GetMenuByTimeslot(string timeslotId, PagingRequest paging)
        {
            try
            {
                var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                                            .FirstOrDefault(x => x.Id == Guid.Parse(timeslotId));

                if (timeslot == null)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                        TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());

                var listMenu = _unitOfWork.Repository<Menu>().GetAll()
                                    .Where(x => x.TimeSlotId == Guid.Parse(timeslotId) && x.IsActive == true)
                                    .ProjectTo<MenuResponse>(_mapper.ConfigurationProvider)
                                    .ToList();
                foreach(var menu in listMenu)
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

                return new BaseResponseViewModel<List<MenuResponse>>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = listMenu
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
