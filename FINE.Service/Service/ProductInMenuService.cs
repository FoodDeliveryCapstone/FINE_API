using System.Linq.Dynamic.Core;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Request.ProductInMenu;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Algorithm;
using ServiceStack.Script;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IProductInMenuService
    {
        Task<BaseResponsePagingViewModel<ProductInMenuResponse>> GetProductInMenu(string menuId, PagingRequest paging);
        //Task<BaseResponsePagingViewModel<ProductInMenuResponse>> GetProductInMenuByStore(int storeId, ProductInMenuResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<dynamic>> AddProductIntoMenu(AddProductToMenuRequest request);
        Task<BaseResponseViewModel<dynamic>> UpdateProductInMenu(string productInMenuId, UpdateProductInMenuRequest request);
        //Task<BaseResponseViewModel<AddProductToMenuResponse>> UpdateAllProductInMenuStatus(UpdateAllProductInMenuStatusRequest request);
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

        public async Task<BaseResponsePagingViewModel<ProductInMenuResponse>> GetProductInMenu(string menuId, PagingRequest paging)
        {
            try
            {
                List<ProductInMenuResponse> listProductInMenu = new List<ProductInMenuResponse>();

                var getProductInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                   .Where(x => x.MenuId == Guid.Parse(menuId));

                var productAttribute = _unitOfWork.Repository<ProductAttribute>().GetAll();
                foreach(var productInMenu in getProductInMenu)
                {
                    ProductInMenuResponse product = new ProductInMenuResponse() 
                    { 
                        Id = productInMenu.Id,
                        ProductId = productInMenu.ProductId,
                        ProductName = productAttribute.FirstOrDefault(x => x.Id == productInMenu.ProductId).Name,
                        MenuId = productInMenu.MenuId,
                        IsActive = productInMenu.IsActive,
                        Status = productInMenu.Status,
                        CreatedAt = productInMenu.CreatedAt,
                        UpdatedAt = productInMenu.UpdatedAt
                    };
                    listProductInMenu.Add(product);
                }
                var productInMenuResponse = listProductInMenu.AsQueryable().PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<ProductInMenuResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = productInMenuResponse.Item1
                    },
                    Data = productInMenuResponse.Item2.ToList()
                };
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }

        //public async Task<BaseResponsePagingViewModel<ProductInMenuResponse>> GetProductInMenuByStore(int storeId, ProductInMenuResponse filter, PagingRequest paging)
        //{
        //    try
        //    {
        //        var store = _unitOfWork.Repository<Store>().GetAll()
        //          .FirstOrDefault(x => x.Id == storeId);
        //        if (store == null)
        //            throw new ErrorResponse(404, (int)StoreErrorEnums.NOT_FOUND,
        //                StoreErrorEnums.NOT_FOUND.GetDisplayName());

        //        var products = _unitOfWork.Repository<ProductInMenu>().GetAll()
        //           .Where(x => x.StoreId == storeId)
        //           .ProjectTo<ProductInMenuResponse>(_mapper.ConfigurationProvider)
        //           .DynamicFilter(filter)
        //           .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

        //        return new BaseResponsePagingViewModel<ProductInMenuResponse>()
        //        {
        //            Metadata = new PagingsMetadata()
        //            {
        //                Page = paging.Page,
        //                Size = paging.PageSize,
        //                Total = products.Item1
        //            },
        //            Data = products.Item2.ToList()
        //        };
        //    }
        //    catch (ErrorResponse ex)
        //    {
        //        throw;
        //    }
        //}

        public async Task<BaseResponseViewModel<dynamic>> AddProductIntoMenu(AddProductToMenuRequest request)
        {
            try
            {
                var getProductAttribute = await _unitOfWork.Repository<ProductAttribute>().GetAll()
                            .Where(x => request.ProductIds.Contains(x.ProductId))
                            .ToListAsync();
                var menu = await _unitOfWork.Repository<Menu>().GetAll()
                            .FirstOrDefaultAsync(x => x.Id == request.MenuId);
                if (menu == null)
                    throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND,
                        MenuErrorEnums.NOT_FOUND.GetDisplayName());

                foreach (var attribute in getProductAttribute)
                {
                    var checkProductInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                       .FirstOrDefault(x => x.ProductId == attribute.ProductId && x.MenuId == request.MenuId);
                    if (checkProductInMenu != null)
                        throw new ErrorResponse(404, (int)ProductInMenuErrorEnums.PRODUCT_ALREADY_IN_MENU,
                            ProductInMenuErrorEnums.PRODUCT_ALREADY_IN_MENU.GetDisplayName());

                    var addProductToMenu = new ProductInMenu()
                    {
                        Id = Guid.NewGuid(),
                        ProductId = attribute.Id,
                        MenuId = menu.Id,
                        Status = (int)ProductInMenuStatusEnum.Avaliable,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                    };

                    await _unitOfWork.Repository<ProductInMenu>().InsertAsync(addProductToMenu);
                }

                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<dynamic>()
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

        public async Task<BaseResponseViewModel<dynamic>> UpdateProductInMenu(string productInMenuId, UpdateProductInMenuRequest request)
        {
            try
            {
                var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                     .FirstOrDefault(x => x.Id == Guid.Parse(productInMenuId));
                if (productInMenu == null)
                    throw new ErrorResponse(404, (int)ProductInMenuErrorEnums.NOT_FOUND,
                        ProductInMenuErrorEnums.NOT_FOUND.GetDisplayName());

                var updateProductInMenu = _mapper.Map<UpdateProductInMenuRequest, ProductInMenu>(request, productInMenu);

                updateProductInMenu.UpdatedAt = DateTime.Now;

                await _unitOfWork.Repository<ProductInMenu>().UpdateDetached(updateProductInMenu);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<dynamic>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    }
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //public async Task<BaseResponseViewModel<AddProductToMenuResponse>> UpdateAllProductInMenuStatus(UpdateAllProductInMenuStatusRequest request)
        //{
        //    try
        //    {
        //        var productsInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll().ToList();
        //        foreach (var productInMenu in productsInMenu)
        //        {
        //            if (productInMenu.IsAvailable == true)
        //            {
        //                productInMenu.IsAvailable = false;
        //            }
        //            else if (productInMenu.IsAvailable == false)
        //            {
        //                productInMenu.IsAvailable = true;
        //            }

        //            await _unitOfWork.Repository<ProductInMenu>().UpdateDetached(productInMenu);
        //        }
        //        await _unitOfWork.CommitAsync();

        //        return new BaseResponseViewModel<AddProductToMenuResponse>()
        //        {
        //            Status = new StatusViewModel()
        //            {
        //                Message = "Success",
        //                Success = true,
        //                ErrorCode = 0
        //            },
        //            //Data = ;
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }

        //}
    }
}
