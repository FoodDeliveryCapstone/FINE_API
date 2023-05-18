using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Request.ProductInMenu;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Algorithm;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IProductInMenuService
    {
        Task<BaseResponseViewModel<ProductInMenuResponse>> GetProductByProductInMenu(int productInMenuId);
        Task<List<ProductInMenuResponse>> GetProductInMenuByStore(int storeId, PagingRequest paging);
    }

    public class ProductInMenuService : IProductInMenuService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;



        public ProductInMenuService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<ProductInMenuResponse>> GetProductByProductInMenu(int productInMenuId)
        {
            try
            {
                var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                   .FirstOrDefault(x => x.Id == productInMenuId);
                if (productInMenu == null)
                    throw new ErrorResponse(404, (int)ProductInMenuErrorEnums.NOT_FOUND_ID,
                        ProductInMenuErrorEnums.NOT_FOUND_ID.GetDisplayName());


                return new BaseResponseViewModel<ProductInMenuResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<ProductInMenuResponse>(productInMenu)
                };
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }

        public async Task<List<ProductInMenuResponse>> GetProductInMenuByStore(int storeId, PagingRequest paging)
        {
            try
            {
                var store = _unitOfWork.Repository<Store>().GetAll()
                  .FirstOrDefault(x => x.Id == storeId);
                if (store == null)
                    throw new ErrorResponse(404, (int)StoreErrorEnums.NOT_FOUND_ID,
                        StoreErrorEnums.NOT_FOUND_ID.GetDisplayName());

                var products = _unitOfWork.Repository<ProductInMenu>().GetAll()
                   .Where(x => x.StoreId == storeId)
                   .ProjectTo<ProductInMenuResponse>(_mapper.ConfigurationProvider)
                   .ToList();

                var result = new List<ProductInMenuResponse>();
                foreach (var item in products)
                {
                    if (!result.Any(x => x.ProductId == item.ProductId))
                    {
                        result.Add(item);
                    }
                }

                return result;
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }
    }
}
