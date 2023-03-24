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
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Algorithm;
using NTQ.Sdk.Core.Utilities;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IProductInMenuService
    {
        Task<BaseResponseViewModel<ProductInMenuResponse>> GetProductByProductInMenu(int productInMenuId);
        Task<List<ProductInMenuResponse>> GetProductInMenuByStore(int storeId, PagingRequest paging);
        Task<BaseResponsePagingViewModel<ProductInMenuBestSellerResponse>> GetProductInMenuBestSeller(PagingRequest paging);
    }

    public class ProductInMenuService : IProductInMenuService
    {
        private readonly FineStgDbContext _context;
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;



        public ProductInMenuService(FineStgDbContext context, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _context = context;
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

        public async Task<BaseResponsePagingViewModel<ProductInMenuBestSellerResponse>> GetProductInMenuBestSeller(PagingRequest paging)
        {
            try
            {
                #region Get Order Detail from 1 month
                DateTime oneMonthFromNow = DateTime.Now.AddMonths(-1);
                var orderDetail = _unitOfWork.Repository<OrderDetail>().GetAll()
                                        .Include(x => x.Order)
                                        .Where(x => x.Order.CheckInDate <= DateTime.Now && x.Order.CheckInDate >= oneMonthFromNow)
                                        .Take(100)
                                        .ToList();
                if (orderDetail == null)
                    throw new ErrorResponse(404, (int)ProductBestSellerErrorEnums.NOT_FOUND_ORDER,
                        ProductBestSellerErrorEnums.NOT_FOUND_ORDER.GetDisplayName());
                #endregion

                #region Find Order Detail that have similar Product greater than 3
                var orderDetailWithSimilarProduct =
                   orderDetail.GroupBy(x => x.ProductInMenuId)
                   .Where(x => x.Count() > 3)
                   .Take(5)
                   .Select(g => new { ProductInMenuId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
                   .ToList();

                if (orderDetailWithSimilarProduct == null)
                    throw new ErrorResponse(404, (int)ProductBestSellerErrorEnums.SIMILAR_PRODUCT_NOT_FOUND,
                        ProductBestSellerErrorEnums.SIMILAR_PRODUCT_NOT_FOUND.GetDisplayName());
                #endregion

                #region Calculate Quantity
                var productBestSellerList = new List<ProductInMenuBestSellerResponse>();
                //var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll().ToList();
                foreach (var product in orderDetailWithSimilarProduct)
                {
                    //find Product in ProductInMenu
                    var productInMenu = _unitOfWork.Repository<ProductInMenu>()
                                            .GetAll()
                                            .FirstOrDefault(x => x.Id == product.ProductInMenuId);

                    if (productInMenu != null)
                    {
                        var productBestSeller = _mapper.Map<ProductInMenuBestSellerResponse>(productInMenu);
                        productBestSeller.Quantity = product.TotalQuantity;
                        productBestSellerList.Add(productBestSeller);
                    }
                }
                return new BaseResponsePagingViewModel<ProductInMenuBestSellerResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = productBestSellerList.Count()
                    },
                    Data = productBestSellerList.ToList()
                };
                #endregion
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }
    }
}
