﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using NTQ.Sdk.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IRevenueService
    {
        Task<BaseResponseViewModel<RevenueResponse>> GetSystemRevenueByMonth(DateFilter filter);
        Task<BaseResponseViewModel<StoreRevenueResponse>> GetStoreRevenueByMonth(int storeId, DateFilter filter);

    }
    public class RevenueService : IRevenueService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RevenueService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseViewModel<RevenueResponse>> GetSystemRevenueByMonth(DateFilter filter)
        {
            #region check date
            var from = filter?.FromDate;
            var to = filter?.ToDate;
            if (from == null && to == null)
            {
                from = Ultils.GetLastAndFirstDateInCurrentMonth().Item1;
                //to = Ultils.GetCurrentDatetime().AddDays(-1);
                to = Ultils.GetCurrentDatetime();
            }

            if (from == null)
            {
                //from = Ultils.GetCurrentDatetime().AddDays(-1);
                from = Ultils.GetCurrentDatetime();
            }

            if (to == null)
            {
                //to = Ultils.GetCurrentDatetime().AddDays(-1);
                to = Ultils.GetCurrentDatetime();
            }
            #endregion

            from = ((DateTime)from).GetStartOfDate();
            to = ((DateTime)to).GetEndOfDate();

            var getSystemRevenue = _unitOfWork.Repository<Order>().GetAll()
                        .Where(x => x.OrderStatus == (int)OrderStatusEnum.Processing
                                && x.CheckInDate >= from 
                                && x.CheckInDate < to);

            var totalSystemRevenue = new RevenueResponse();

            totalSystemRevenue.TotalRevenueBeforeDiscount = getSystemRevenue.Sum(x => x.TotalAmount);
            totalSystemRevenue.TotalDiscount = getSystemRevenue.Sum(x => x.Discount);
            totalSystemRevenue.TotalShippingFee = getSystemRevenue.Sum(x => x.ShippingFee);
            totalSystemRevenue.TotalRevenueAfterDiscount = getSystemRevenue.Sum(x => x.FinalAmount);

            return new BaseResponseViewModel<RevenueResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<RevenueResponse>(totalSystemRevenue)
            };

        }

        public async Task<BaseResponseViewModel<StoreRevenueResponse>> GetStoreRevenueByMonth(int storeId, DateFilter filter)
        {
            #region check date
            var from = filter?.FromDate;
            var to = filter?.ToDate;
            if (from == null && to == null)
            {
                from = Ultils.GetLastAndFirstDateInCurrentMonth().Item1;
                //to = Ultils.GetCurrentDatetime().AddDays(-1);
                to = Ultils.GetCurrentDatetime();
            }

            if (from == null)
            {
                //from = Ultils.GetCurrentDatetime().AddDays(-1);
                from = Ultils.GetCurrentDatetime();
            }

            if (to == null)
            {
                //to = Ultils.GetCurrentDatetime().AddDays(-1);
                to = Ultils.GetCurrentDatetime();
            }
            #endregion

            from = ((DateTime)from).GetStartOfDate();
            to = ((DateTime)to).GetEndOfDate();

            var getStoreRevenue = _unitOfWork.Repository<Order>().GetAll()
                        .Where(x => x.StoreId == storeId
                                && x.OrderStatus == (int)OrderStatusEnum.Processing
                                && x.CheckInDate >= from
                                && x.CheckInDate < to);

            var store = _unitOfWork.Repository<Store>().GetAll()
                        .FirstOrDefault(x => x.Id == storeId);

            var totalStoreRevenue = new StoreRevenueResponse();

            totalStoreRevenue.StoreId = store.Id;
            totalStoreRevenue.StoreName = store.StoreName;
            totalStoreRevenue.TotalRevenueBeforeDiscount = getStoreRevenue.Sum(x => x.TotalAmount);
            totalStoreRevenue.TotalDiscount = getStoreRevenue.Sum(x => x.Discount);
            totalStoreRevenue.TotalRevenueAfterDiscount = getStoreRevenue.Sum(x => x.FinalAmount);

            return new BaseResponseViewModel<StoreRevenueResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StoreRevenueResponse>(totalStoreRevenue)
            };


        }
    }
}
