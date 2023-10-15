using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Destination;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IDestinationService
    {
        //Task<BaseResponsePagingViewModel<DestinationResponse>> GetListDestination(DestinationResponse request, PagingRequest paging);
        Task<BaseResponseViewModel<DestinationResponse>> GetDestinationById(string id);
        Task<BaseResponseViewModel<DestinationResponse>> CreateDestination(CreateDestinationRequest request);
        Task<BaseResponseViewModel<DestinationResponse>> UpdateDestination(string id, UpdateDestinationRequest request);
    }

    public class DestinationService : IDestinationService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public DestinationService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        //public async Task<BaseResponsePagingViewModel<DestinationResponse>> GetListDestination(DestinationResponse filter,
        //    PagingRequest paging)
        //{
        //    try
        //    {
        //        var Destination = _unitOfWork.Repository<Destination>().GetAll()
        //            .ProjectTo<DestinationResponse>(_mapper.ConfigurationProvider)
        //            .DynamicFilter(filter)
        //            .DynamicSort(filter)
        //            .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

        //        return new BaseResponsePagingViewModel<DestinationResponse>()
        //        {
        //            Metadata = new PagingsMetadata()
        //            {
        //                Page = paging.Page,
        //                Size = paging.PageSize,
        //                Total = Destination.Item1
        //            },
        //            Data = Destination.Item2.ToList()
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}

        public async Task<BaseResponseViewModel<DestinationResponse>> GetDestinationById(string id)
        {
            try
            {
                var destinationId = Guid.Parse(id);
                var Destination = _unitOfWork.Repository<Destination>().GetAll()
                    .Include(x => x.TimeSlots)
                    .FirstOrDefault(x => x.Id == destinationId);

                if (Destination == null)
                    throw new ErrorResponse(404, (int)DestinationErrorEnums.NOT_FOUND,
                        DestinationErrorEnums.NOT_FOUND.GetDisplayName());

                return new BaseResponseViewModel<DestinationResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<DestinationResponse>(Destination)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<DestinationResponse>> CreateDestination(CreateDestinationRequest request)
        {
            try
            {
                var checkCode = _unitOfWork.Repository<Destination>().GetAll().Any(x => x.Code == request.Code);
                if (checkCode)
                    throw new ErrorResponse(400, (int)DestinationErrorEnums.Destination_CODE_EXIST,
                            DestinationErrorEnums.Destination_CODE_EXIST.GetDisplayName());

                var destination = _mapper.Map<CreateDestinationRequest, Destination>(request);

                destination.Id = Guid.NewGuid();
                destination.IsActive = true;
                destination.CreateAt = DateTime.Now;

                await _unitOfWork.Repository<Destination>().InsertAsync(destination);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<DestinationResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<DestinationResponse>> UpdateDestination(string id, UpdateDestinationRequest request)
        {
            try
            {
                var destinationId = Guid.Parse(id);
                var destination = _unitOfWork.Repository<Destination>()
                     .Find(c => c.Id == destinationId);

                if (destination == null)
                    throw new ErrorResponse(404, (int)DestinationErrorEnums.NOT_FOUND,
                        DestinationErrorEnums.NOT_FOUND.GetDisplayName());

                var updateDestination = _mapper.Map<UpdateDestinationRequest, Destination>(request, destination);
                updateDestination.UpdateAt = DateTime.Now;

                await _unitOfWork.Repository<Destination>().UpdateDetached(updateDestination);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<DestinationResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<DestinationResponse>(updateDestination)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}