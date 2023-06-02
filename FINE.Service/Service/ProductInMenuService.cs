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
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IProductInMenuService
    {
        Task<BaseResponseViewModel<ProductInMenuResponse>> GetProductInMenuById(int productInMenuId);
        Task<BaseResponsePagingViewModel<ProductInMenuResponse>> GetProductInMenuByStore(int storeId,ProductInMenuResponse filter, PagingRequest paging);
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

        public async Task<BaseResponseViewModel<ProductInMenuResponse>> GetProductInMenuById(int productInMenuId)
        {
            try
            {
                var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                   .FirstOrDefault(x => x.Id == productInMenuId);

                if (productInMenu == null)
                    throw new ErrorResponse(404, (int)ProductInMenuErrorEnums.NOT_FOUND,
                        ProductInMenuErrorEnums.NOT_FOUND.GetDisplayName());


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

        public async Task<BaseResponsePagingViewModel<ProductInMenuResponse>> GetProductInMenuByStore(int storeId, ProductInMenuResponse filter ,PagingRequest paging)
        {
            try
            {
                var store = _unitOfWork.Repository<Store>().GetAll()
                  .FirstOrDefault(x => x.Id == storeId);
                if (store == null)
                    throw new ErrorResponse(404, (int)StoreErrorEnums.NOT_FOUND,
                        StoreErrorEnums.NOT_FOUND.GetDisplayName());

                var products = _unitOfWork.Repository<ProductInMenu>().GetAll()
                   .Where(x => x.StoreId == storeId)
                   .ProjectTo<ProductInMenuResponse>(_mapper.ConfigurationProvider)
                   .DynamicFilter(filter)
                   .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<ProductInMenuResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = products.Item1
                    },
                    Data = products.Item2.ToList()
                };
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
                foreach (var productInMenu in request.Products)
                {
                    var menu = _unitOfWork.Repository<Menu>().GetAll()
                        .FirstOrDefault(x => x.Id == request.MenuId);
                    if (menu == null)
                        throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND,
                            MenuErrorEnums.NOT_FOUND.GetDisplayName());

                    var product = _unitOfWork.Repository<Product>().GetAll()
                        .FirstOrDefault(x => x.Id == productInMenu.ProductId);
                    if (product == null)
                        throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND,
                            ProductErrorEnums.NOT_FOUND.GetDisplayName());

                    var checkProductInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                       .FirstOrDefault(x => x.ProductId == productInMenu.ProductId && x.MenuId == request.MenuId);
                    if (checkProductInMenu != null)
                        throw new ErrorResponse(404, (int)ProductInMenuErrorEnums.PRODUCT_ALREADY_IN_MENU,
                            ProductInMenuErrorEnums.PRODUCT_ALREADY_IN_MENU.GetDisplayName());

                    var addProductToMenu = new ProductInMenu() 
                    {
                        ProductId = productInMenu.ProductId,
                        Price = productInMenu.Price,
                        MenuId = menu.Id,
                        StoreId = product.StoreId,
                        CreatedAt = DateTime.Now,
                        Status = (int)ProductInMenuStatusEnum.Wait,
                        IsAvailable = true,
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
                    throw new ErrorResponse(404, (int)ProductInMenuErrorEnums.NOT_FOUND,
                        ProductInMenuErrorEnums.NOT_FOUND.GetDisplayName());

                var updateProductInMenu = _mapper.Map<UpdateProductInMenuRequest, ProductInMenu>(request, productInMenu);

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
