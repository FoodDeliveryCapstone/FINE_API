using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.Service
{
    public interface ITransactionService
    {
        Task<BaseResponsePagingViewModel<TransactionResponse>> GetAllTransaction(PagingRequest paging);
        Task<BaseResponsePagingViewModel<RefundTransactionResponse>> GetRefundTransaction(PagingRequest paging);
    }

    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public TransactionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponsePagingViewModel<TransactionResponse>> GetAllTransaction(PagingRequest paging)
        {
            try
            {
                var transactions = _unitOfWork.Repository<Transaction>().GetAll()
                                .OrderByDescending(x => x.CreatedAt)
                                .ProjectTo<TransactionResponse>(_mapper.ConfigurationProvider)
                                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<TransactionResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = transactions.Item1
                    },
                    Data = transactions.Item2.ToList()
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<RefundTransactionResponse>> GetRefundTransaction(PagingRequest paging)
        {
            try
            {
                var otherAmounts = _unitOfWork.Repository<OtherAmount>().GetAll()
                                .Where(x => x.Type == (int)OtherAmountTypeEnum.Refund)
                                .ProjectTo<RefundTransactionResponse>(_mapper.ConfigurationProvider)
                                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<RefundTransactionResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = otherAmounts.Item1
                    },
                    Data = otherAmounts.Item2.ToList()
                };
            }
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
