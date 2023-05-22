using System.Linq.Dynamic.Core;
using System.Net.NetworkInformation;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Request.ProductInMenu;
using FINE.Service.DTO.Request.TimeSlot;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service;

public interface IProductToMenuService
{
    Task<BaseResponseViewModel<ProductInMenuResponse>> AddProductIntoMenu(AddProductToMenuRequest request);
    Task<BaseResponseViewModel<ProductInMenuResponse>> UpdateProductInMenu(int productInMenuId, UpdateProductInMenuRequest request);
}

public class ProductToMenuService : IProductToMenuService
{
    private IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public ProductToMenuService(IMapper mapper, IUnitOfWork unitOfWork)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponseViewModel<ProductInMenuResponse>> AddProductIntoMenu(AddProductToMenuRequest request)
    {
        try
        {
            #region check product and menu exsist
            var menu = _unitOfWork.Repository<Menu>().GetAll()
                .FirstOrDefault(x => x.Id == request.MenuId);
            if (menu == null)
                throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND_ID,
                    MenuErrorEnums.NOT_FOUND_ID.GetDisplayName());

            var product = _unitOfWork.Repository<Product>().GetAll()
                .FirstOrDefault(x => x.Id == request.ProductId);
            if (product == null)
                throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND_ID,
                    ProductErrorEnums.NOT_FOUND_ID.GetDisplayName());

            var checkProductInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                .FirstOrDefault(x => x.ProductId == request.ProductId && x.MenuId == request.MenuId);
            if (checkProductInMenu != null)
                throw new ErrorResponse(404, (int)ProductInMenuErrorEnums.PRODUCT_ALREADY_IN_MENU,
                    ProductInMenuErrorEnums.PRODUCT_ALREADY_IN_MENU.GetDisplayName());
            #endregion

            var productInMenu = _mapper.Map<AddProductToMenuRequest, ProductInMenu>(request);

            productInMenu.ProductId = product.Id;
            productInMenu.StoreId = product.StoreId;
            productInMenu.CreatedAt = DateTime.Now;
            productInMenu.IsAvailable = false;

            await _unitOfWork.Repository<ProductInMenu>().InsertAsync(productInMenu);
            await _unitOfWork.CommitAsync();

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
}