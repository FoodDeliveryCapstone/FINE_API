using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Request.Menu;
using FINE.Service.DTO.Request.Store;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using NTQ.Sdk.Core.Utilities;
using static FINE.Service.Helpers.ErrorEnum;


namespace FINE.Service.Service
{
    public interface IMenuService
    {
        Task<BaseResponsePagingViewModel<MenuResponse>> GetMenus(MenuResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<MenuResponse>> GetMenuById(int menuId);
        Task<BaseResponsePagingViewModel<MenuResponse>> GetMenuByTimeslot(int timeslotId, PagingRequest paging);
        Task<BaseResponseViewModel<MenuResponse>> CreateMenu(CreateMenuRequest request);
        Task<BaseResponseViewModel<MenuResponse>> UpdateMenu(int menuId, UpdateMenuRequest request);
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

        public async Task<BaseResponseViewModel<MenuResponse>> CreateMenu(CreateMenuRequest request)
        {
            var menu = _mapper.Map<CreateMenuRequest, Menu>(request);

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

        public async Task<BaseResponseViewModel<MenuResponse>> GetMenuById(int menuId)
        {
            var menu = _unitOfWork.Repository<Menu>().GetAll()
                                        .FirstOrDefault(x => x.Id == menuId);
            if (menu == null)
                throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND_ID,
                                    MenuErrorEnums.NOT_FOUND_ID.GetDisplayName());
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

        public async Task<BaseResponsePagingViewModel<MenuResponse>> GetMenus(MenuResponse filter, PagingRequest paging)
        {
            var menu = _unitOfWork.Repository<Menu>().GetAll()
                                    .ProjectTo<MenuResponse>(_mapper.ConfigurationProvider)
                                    .DynamicFilter(filter)
                                    .DynamicSort(filter)
                                    .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                    Constants.DefaultPaging);

            return new BaseResponsePagingViewModel<MenuResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = menu.Item1
                },
                Data = menu.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<MenuResponse>> UpdateMenu(int menuId, UpdateMenuRequest request)
        {
            var menu = _unitOfWork.Repository<Menu>()
                 .Find(c => c.Id == menuId);

            if (menu == null)
                throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND_ID,
                    MenuErrorEnums.NOT_FOUND_ID.GetDisplayName());

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
                Data = _mapper.Map<MenuResponse>(menu)
            };
        }

        public async Task<BaseResponsePagingViewModel<MenuResponse>> GetMenuByTimeslot(int timeslotId, PagingRequest paging)
        {
            var menu = _unitOfWork.Repository<Menu>().GetAll()
                .Where(x => x.TimeSlotId == timeslotId)
                .ProjectTo<MenuResponse>(_mapper.ConfigurationProvider)
                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

            var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                          .FirstOrDefault(x => x.Id == timeslotId);

            if (timeslot == null)
                throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND_ID,
                    TimeSlotErrorEnums.NOT_FOUND_ID.GetDisplayName());

            return new BaseResponsePagingViewModel<MenuResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = menu.Item1
                },
                Data = menu.Item2.ToList()
            };
        }
    }
}
