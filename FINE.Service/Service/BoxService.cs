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
        Task<BaseResponsePagingViewModel<BoxResponse>> GetBoxByStation(string stationId, BoxResponse filter, PagingRequest paging);

    }

    public class BoxService : IBoxService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public BoxService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
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
                    .OrderBy(x => x.CreateAt)
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
