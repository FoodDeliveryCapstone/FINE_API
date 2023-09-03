﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Box;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IOrderDetailService
    {
        Task<BaseResponsePagingViewModel<OrderDetailResponse>> GetOrdersDetailByStore(string storeId, PagingRequest paging);
        Task<BaseResponsePagingViewModel<OrderByStoreResponse>> GetStaffOrderDetail(string storeId);

    }

    public class OrderDetailService : IOrderDetailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public OrderDetailService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponsePagingViewModel<OrderDetailResponse>> GetOrdersDetailByStore(string storeId, PagingRequest paging)
        {
            try
            {
                var order = _unitOfWork.Repository<OrderDetail>().GetAll()
                                        .Where(x => x.StoreId == Guid.Parse(storeId))
                                        .OrderBy(x => x.OrderId)
                                        .ThenBy(x => x.Order.CheckInDate)
                                        .ProjectTo<OrderDetailResponse>(_mapper.ConfigurationProvider)
                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                        Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<OrderDetailResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = order.Item1
                    },
                    Data = order.Item2.ToList()

                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<OrderByStoreResponse>> GetStaffOrderDetail(string storeId)
        {
            try
            {
                // Get from Redis
                List<OrderByStoreResponse> orderResponse = await ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.GET, storeId);
                orderResponse = orderResponse.Where(x => x.StoreId == Guid.Parse(storeId))
                                             .OrderByDescending(x => x.CheckInDate)
                                             .ToList(); 


                return new BaseResponsePagingViewModel<OrderByStoreResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {                     
                        Total = orderResponse.Count()
                    },
                    Data = orderResponse
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
