using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Request.ProductAttribute;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IProductAttributeService
    {
        Task<BaseResponseViewModel<ProductResponse>> CreateProductAttribute(string productId, List<CreateProductAttributeRequest> request);
        Task<BaseResponseViewModel<ProductResponse>> UpdateProductAttribute(string productAttributeId, UpdateProductAttributeRequest request);
    }

    public class ProductAttributeService : IProductAttributeService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public ProductAttributeService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<ProductResponse>> UpdateProductAttribute(string productAttributeId, UpdateProductAttributeRequest request)
        {
            try
            {
                var getAllProductAttribute = _unitOfWork.Repository<ProductAttribute>().GetAll();
                var productAttribute = getAllProductAttribute.FirstOrDefault(x => x.Id == Guid.Parse(productAttributeId));
                if (productAttribute == null)
                    throw new ErrorResponse(404, (int)ProductAttributeErrorEnums.NOT_FOUND,
                        ProductAttributeErrorEnums.NOT_FOUND.GetDisplayName());

                var checkCode = getAllProductAttribute.FirstOrDefault(x => x.Code == request.Code && x.Id != Guid.Parse(productAttributeId));
                if (checkCode != null)
                    throw new ErrorResponse(404, (int)ProductAttributeErrorEnums.PRODUCT_CODE_EXIST,
                       ProductAttributeErrorEnums.PRODUCT_CODE_EXIST.GetDisplayName());

                var updateProductAttribute = _mapper.Map<UpdateProductAttributeRequest, ProductAttribute>(request, productAttribute);

                updateProductAttribute.UpdateAt = DateTime.Now;

                await _unitOfWork.Repository<ProductAttribute>().UpdateDetached(updateProductAttribute);
                await _unitOfWork.CommitAsync();

                var getProduct = await _unitOfWork.Repository<Product>().GetAll().FirstOrDefaultAsync(x => x.Id == productAttribute.ProductId);

                return new BaseResponseViewModel<ProductResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<ProductResponse>(getProduct)
                };
            }
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<ProductResponse>> CreateProductAttribute(string productId, List<CreateProductAttributeRequest> request)
        {
            try
            {
                var product = await _unitOfWork.Repository<Product>().GetAll().FirstOrDefaultAsync(x => x.Id == Guid.Parse(productId));
                if (product == null)
                    throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND,
                       ProductErrorEnums.NOT_FOUND.GetDisplayName());

                foreach (var attribute in request)
                {
                    var productAttribute = _mapper.Map<CreateProductAttributeRequest, ProductAttribute>(attribute);

                    productAttribute.Id = Guid.NewGuid();
                    productAttribute.ProductId = product.Id;
                    productAttribute.IsActive = true;
                    productAttribute.RotationType = (int)attribute.RotationType;
                    productAttribute.CreateAt = DateTime.Now;
                    if (attribute.Size is null || attribute.Size == "")
                    {
                        productAttribute.Size = null;
                        productAttribute.Name = product.ProductName;
                        productAttribute.Code = product.ProductCode;

                    }
                    else
                    {
                        productAttribute.Name = product.ProductName + "(" + attribute.Size + ")";
                        productAttribute.Code = product.ProductCode + attribute.Size.ToLower();
                    }

                    await _unitOfWork.Repository<ProductAttribute>().InsertAsync(productAttribute);
                }
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<ProductResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<ProductResponse>(product)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
