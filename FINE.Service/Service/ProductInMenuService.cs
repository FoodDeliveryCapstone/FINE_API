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
        Task<BaseResponseViewModel<ProductInMenuResponse>> AddProductIntoMenu(AddProductToMenuRequest request);
        Task<BaseResponseViewModel<ProductInMenuResponse>> UpdateProductInMenu(int productInMenuId, UpdateProductInMenuRequest request);
        Task<BaseResponseViewModel<AddProductToMenuResponse>> UpdateAllProductInMenuStatus(UpdateAllProductInMenuStatusRequest request);
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

        public async Task<BaseResponseViewModel<ProductInMenuResponse>> AddProductIntoMenu(AddProductToMenuRequest request)
        {
            try
            {
               

                foreach (var product in request.addProducts)
                {
                    var productInMenu = _mapper.Map<AddProductToMenuRequest, ProductInMenu>(request);
                    var menu = _unitOfWork.Repository<Menu>().GetAll()
                        .FirstOrDefault(x => x.Id == request.MenuId);
                    if (menu == null)
                        throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND_ID,
                            MenuErrorEnums.NOT_FOUND_ID.GetDisplayName());

                    var products = _unitOfWork.Repository<Product>().GetAll()
                        .FirstOrDefault(x => x.Id == product.ProductId);
                    if (products == null)
                        throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND_ID,
                            ProductErrorEnums.NOT_FOUND_ID.GetDisplayName());

                    var checkProductInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                       .FirstOrDefault(x => x.ProductId == product.ProductId && x.MenuId == request.MenuId);
                    if (checkProductInMenu != null)
                        throw new ErrorResponse(404, (int)ProductInMenuErrorEnums.PRODUCT_ALREADY_IN_MENU,
                            ProductInMenuErrorEnums.PRODUCT_ALREADY_IN_MENU.GetDisplayName());

                    var addProductToMenu = new ProductInMenu() 
                    {
                        ProductId = product.ProductId,
                        Price = product.Price,
                        Status = product.Status,
                        MenuId = menu.Id,
                        StoreId = products.StoreId,
                        CreatedAt = DateTime.Now,
                        IsAvailable = false,
                    };

                    await _unitOfWork.Repository<ProductInMenu>().InsertAsync(addProductToMenu);
                    await _unitOfWork.CommitAsync();
                }              

                return new BaseResponseViewModel<ProductInMenuResponse>()
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
                throw;
            }
        }

        public async Task<BaseResponseViewModel<ProductInMenuResponse>> UpdateProductInMenu(int productInMenuId, UpdateProductInMenuRequest request)
        {
            try
            {
                var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                     .FirstOrDefault(x => x.Id == productInMenuId);
                if (productInMenu == null)
                    throw new ErrorResponse(404, (int)ProductInMenuErrorEnums.NOT_FOUND_ID,
                        ProductInMenuErrorEnums.NOT_FOUND_ID.GetDisplayName());

                var updateProductInMenu = _mapper.Map<UpdateProductInMenuRequest, ProductInMenu>(request, productInMenu);

                updateProductInMenu.ProductId = productInMenu.ProductId;
                updateProductInMenu.MenuId = productInMenu.MenuId;
                updateProductInMenu.StoreId = productInMenu.StoreId;
                updateProductInMenu.UpdatedAt = DateTime.Now;

                await _unitOfWork.Repository<ProductInMenu>().UpdateDetached(updateProductInMenu);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<ProductInMenuResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<ProductInMenuResponse>(updateProductInMenu)
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<AddProductToMenuResponse>> UpdateAllProductInMenuStatus(UpdateAllProductInMenuStatusRequest request)
        {
            try
            {
                var productsInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll().ToList();
                foreach (var productInMenu in productsInMenu)
                {
                    if (productInMenu.IsAvailable == true)
                    {
                        productInMenu.IsAvailable = false;
                    }
                    else if (productInMenu.IsAvailable == false)
                    {
                        productInMenu.IsAvailable = true;
                    }

                    await _unitOfWork.Repository<ProductInMenu>().UpdateDetached(productInMenu);
                }
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<AddProductToMenuResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    //Data = ;
                };
            }
            catch (Exception ex)
            {
                throw;
            }

        }
    }
}
