using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Box;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using System.Drawing;
using ZXing.QrCode;
using ZXing;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;
using ZXing.Windows.Compatibility;
using AutoMapper.QueryableExtensions;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request.Order;
using Microsoft.EntityFrameworkCore;

namespace FINE.Service.Service
{
    public interface IBoxService
    {
        Task<BaseResponseViewModel<OrderBoxResponse>> AddOrderToBox(string stationId, string key, AddOrderToBoxRequest request);
        Task<BaseResponsePagingViewModel<BoxResponse>> GetBoxByStation(string stationId, BoxResponse filter, PagingRequest paging);

    }

    public class BoxService : IBoxService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IStaffService _staffService;
        public BoxService(IUnitOfWork unitOfWork, IMapper mapper, IStaffService staffService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _staffService = staffService;
        }

        public async Task<BaseResponseViewModel<OrderBoxResponse>> AddOrderToBox(string stationId, string key, AddOrderToBoxRequest request)
        {
            try
            {
                //var key = Utils.GenerateRandomCode(10);
                var activeBox = await _unitOfWork.Repository<Box>().GetAll()
                    .Where(x => x.IsActive == true && x.StationId == Guid.Parse(stationId))
                    .ToListAsync();
                if (activeBox == null)
                    throw new ErrorResponse(404, (int)BoxErrorEnums.BOX_NOT_AVAILABLE,
                       BoxErrorEnums.BOX_NOT_AVAILABLE.GetDisplayName());

                var box = activeBox.FirstOrDefault();

                var order = _unitOfWork.Repository<Order>().GetAll()
                                .FirstOrDefault(x => x.Id == request.OrderId);                   
                if (order == null)
                    throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND,
                        OrderErrorEnums.NOT_FOUND.GetDisplayName());
                if (order.OrderStatus != (int)OrderStatusEnum.Delivering)
                    throw new ErrorResponse(400, (int)OrderErrorEnums.CANNOT_UPDATE_ORDER,
                        OrderErrorEnums.CANNOT_UPDATE_ORDER.GetDisplayName());

                var orderBox = new OrderBox()
                {
                    Id = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    BoxId = box.Id,
                    Key = key,
                    Status = (int)OrderBoxStatusEnum.NotPicked,
                    CreateAt = DateTime.Now,
                };
                await _unitOfWork.Repository<OrderBox>().InsertAsync(orderBox);
                await _unitOfWork.CommitAsync();

                box.IsActive = false;
                await _unitOfWork.Repository<Box>().UpdateDetached(box);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<OrderBoxResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<BoxResponse>> GetBoxByStation(string stationId, BoxResponse filter, PagingRequest paging)
        {
            try 
            { 
                var station = _unitOfWork.Repository<Station>().GetAll()
                    .FirstOrDefault(x => x.Id == Guid.Parse(stationId));
                if (station == null)
                    throw new ErrorResponse(404, (int)StationErrorEnums.NOT_FOUND,
                        StationErrorEnums.NOT_FOUND.GetDisplayName());

                var box = _unitOfWork.Repository<Box>().GetAll()
                    .Where(x => x.StationId == Guid.Parse(stationId))
                    .ProjectTo<BoxResponse>(_mapper.ConfigurationProvider)
                    .DynamicFilter(filter)
                    .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<BoxResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = box.Item1
                    },
                    Data = box.Item2.ToList()
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
