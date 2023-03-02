﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using NTQ.Sdk.Core.Utilities;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface ICampusService
    {
        Task<BaseResponsePagingViewModel<CampusResponse>> GetListCampus(CampusResponse request, PagingRequest paging);
        Task<BaseResponseViewModel<CampusResponse>> GetCampusById(int campusId);
        Task<BaseResponseViewModel<CampusResponse>> CreateCampus(CreateCampusRequest request);
        Task<BaseResponseViewModel<CampusResponse>> UpdateCampus(int campusId, UpdateCampusRequest request);
    }

    public class CampusService : ICampusService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public CampusService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponsePagingViewModel<CampusResponse>> GetListCampus(CampusResponse filter, PagingRequest paging)
        {
            try
            {
                var campus = _unitOfWork.Repository<Campus>().GetAll()
                                    .ProjectTo<CampusResponse>(_mapper.ConfigurationProvider)
                                    .DynamicFilter(filter)
                                    .DynamicSort(filter)
                                    .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<CampusResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = campus.Item1
                    },
                    Data = campus.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<CampusResponse>> GetCampusById(int CampusId)
        {
            var campus = _unitOfWork.Repository<Campus>().GetAll()
                          .FirstOrDefault(x => x.Id == CampusId);

            if (campus == null)
                throw new ErrorResponse(404, (int)CampusErrorEnums.NOT_FOUND_ID,
                    CampusErrorEnums.NOT_FOUND_ID.GetDisplayName());

            return new BaseResponseViewModel<CampusResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<CampusResponse>(campus)
            };
        }

        public async Task<BaseResponseViewModel<CampusResponse>> CreateCampus(CreateCampusRequest request)
        {
            var campus = _mapper.Map<CreateCampusRequest, Campus>(request);

            campus.CreateAt = DateTime.Now;

            await _unitOfWork.Repository<Campus>().InsertAsync(campus);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<CampusResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                }
            };
        }

        public async Task<BaseResponseViewModel<CampusResponse>> UpdateCampus(int CampusId, UpdateCampusRequest request)
        {
            var campus = _unitOfWork.Repository<Campus>()
                 .Find(c => c.Id == CampusId);

            if (campus == null)
                throw new ErrorResponse(404, (int)CampusErrorEnums.NOT_FOUND_ID,
                    CampusErrorEnums.NOT_FOUND_ID.GetDisplayName());

            var updateCampus = _mapper.Map<UpdateCampusRequest, Campus>(request, campus);
            updateCampus.UpdateAt = DateTime.Now;

            await _unitOfWork.Repository<Campus>().UpdateDetached(updateCampus);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<CampusResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<CampusResponse>(campus)
            };

        }
    }
}
