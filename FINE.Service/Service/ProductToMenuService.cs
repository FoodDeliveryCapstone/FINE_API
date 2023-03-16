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
using Microsoft.EntityFrameworkCore;
using NTQ.Sdk.Core.Utilities;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service;

public interface IProductToMenuService
{
    Task<BaseResponseViewModel<AddProductToMenuResponse>> AddProductIntoMenu(AddProductToMenuRequest request);
    Task<BaseResponseViewModel<AddProductToMenuResponse>> UpdateProductInMenu(UpdateProductInMenuRequest request);
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

    public async Task<BaseResponseViewModel<AddProductToMenuResponse>> AddProductIntoMenu(AddProductToMenuRequest request)
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
        #endregion

        var productInMenu = _mapper.Map<AddProductToMenuRequest, ProductInMenu>(request);

        productInMenu.ProductId = product.Id;
        productInMenu.StoreId = product.StoreId;
        productInMenu.CreatedAt = DateTime.Now;
        productInMenu.IsAvailable = true;

        await _unitOfWork.Repository<ProductInMenu>().InsertAsync(productInMenu);
        await _unitOfWork.CommitAsync();

        return new BaseResponseViewModel<AddProductToMenuResponse>()
        {
            Status = new StatusViewModel()
            {
                Message = "Success",
                Success = true,
                ErrorCode = 0
            },
            Data = _mapper.Map<AddProductToMenuResponse>(productInMenu)
        };
    }

    public async Task<BaseResponseViewModel<AddProductToMenuResponse>> UpdateProductInMenu(UpdateProductInMenuRequest request)
    {
        try
        {
            var product = _unitOfWork.Repository<Product>().GetAll()
                 .FirstOrDefault(x => x.Id == request.ProductId);
            if (product == null)
                throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND_ID,
                    ProductErrorEnums.NOT_FOUND_ID.GetDisplayName());
            var menu = _unitOfWork.Repository<Menu>().GetAll()
                .FirstOrDefault(x => x.Id == request.MenuId);
            if (menu == null)
                throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND_ID,
                    MenuErrorEnums.NOT_FOUND_ID.GetDisplayName());

            var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                 .FirstOrDefault(x => x.ProductId == request.ProductId);

            var updateProductInMenu = _mapper.Map<UpdateProductInMenuRequest, ProductInMenu>(request, productInMenu);

            updateProductInMenu.ProductId = product.Id;
            updateProductInMenu.StoreId = product.StoreId;
            updateProductInMenu.UpdatedAt= DateTime.Now;

            await _unitOfWork.Repository<ProductInMenu>().UpdateDetached(updateProductInMenu);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<AddProductToMenuResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<AddProductToMenuResponse>(updateProductInMenu)
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}