using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure.Core;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Box;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Request.ProductInMenu;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Algorithm;
using Newtonsoft.Json;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IProductService
    {
        Task<BaseResponseViewModel<ProductResponse>> GetProductById(string productId);
        Task<BaseResponseViewModel<ProductResponse>> CreateProduct(CreateProductRequest request);
        Task<BaseResponseViewModel<ProductResponse>> UpdateProduct(string productId, UpdateProductRequest request);
    }

    public class ProductService : IProductService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<ProductResponse>> GetProductById(string productId)
        {
            try
            {
                var product = _unitOfWork.Repository<Product>().GetAll()
                   .FirstOrDefault(x => x.Id == Guid.Parse(productId));

                if (product == null)
                    throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND,
                        ProductErrorEnums.NOT_FOUND.GetDisplayName());

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

        public async Task<BaseResponseViewModel<ProductResponse>> CreateProduct(CreateProductRequest request)
        {
            try 
            {
                var checkCode = await _unitOfWork.Repository<Product>().GetAll().FirstOrDefaultAsync(x => x.ProductCode == request.ProductCode);
                if (checkCode != null)
                    throw new ErrorResponse(404, (int)ProductErrorEnums.PRODUCT_CODE_EXIST,
                       ProductErrorEnums.PRODUCT_CODE_EXIST.GetDisplayName());

                var product = _mapper.Map<CreateProductRequest, Product>(request);

                product.Id = Guid.NewGuid();
                product.IsActive = true;
                product.CreateAt = DateTime.Now;

                await _unitOfWork.Repository<Product>().InsertAsync(product);

                if(request.ProductAttribute is not null)
                {
                    foreach (var attribute in request.ProductAttribute)
                    {
                        var productAttribute = _mapper.Map<CreateProductAttributeRequest, ProductAttribute>(attribute);

                        productAttribute.Id = Guid.NewGuid();
                        productAttribute.ProductId = product.Id;
                        productAttribute.IsActive = true;
                        productAttribute.RotationType = (int)attribute.RotationType;
                        productAttribute.CreateAt = DateTime.Now;
                        if(attribute.Size is null || attribute.Size == "")
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
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponseViewModel<ProductResponse>> UpdateProduct(string productId, UpdateProductRequest request)
        {
            try
            {
                var getAllProduct = _unitOfWork.Repository<Product>().GetAll();
                var product = getAllProduct.FirstOrDefault(x => x.Id == Guid.Parse(productId));
                if (product == null)
                    throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND,
                        ProductErrorEnums.NOT_FOUND.GetDisplayName());

                var checkCode = getAllProduct.FirstOrDefault(x => x.ProductCode == request.ProductCode && x.Id != Guid.Parse(productId));
                if (checkCode != null)
                    throw new ErrorResponse(404, (int)ProductErrorEnums.PRODUCT_CODE_EXIST,
                       ProductErrorEnums.PRODUCT_CODE_EXIST.GetDisplayName());

                var updateProduct = _mapper.Map<UpdateProductRequest, Product>(request, product);

                updateProduct.UpdateAt = DateTime.Now;
                var productAttribute = await _unitOfWork.Repository<ProductAttribute>().GetAll().Where(x => x.ProductId == Guid.Parse(productId)).ToListAsync();
                if (request.IsActive == false)
                {                   
                    foreach(var attribute in productAttribute)
                    {
                        attribute.IsActive = false;
                        attribute.UpdateAt = DateTime.Now;
                    }
                }
                else if (request.IsActive == true)
                {
                    foreach (var attribute in productAttribute)
                    {
                        if (attribute.IsActive == false)
                        {
                            attribute.IsActive = true;
                            attribute.UpdateAt = DateTime.Now;
                        }
                    }
                }

                await _unitOfWork.Repository<Product>().UpdateDetached(updateProduct);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<ProductResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<ProductResponse>(updateProduct)
                };
            }               
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}