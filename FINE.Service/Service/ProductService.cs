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
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Algorithm;
using Newtonsoft.Json;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IProductService
    {
        Task<BaseResponseViewModel<ProductResponse>> GetProductById(string productId);
        Task<BaseResponseViewModel<ProductResponse>> CreateProduct(CreateProductRequest request);
        Task<BaseResponseViewModel<ProductResponse>> UpdateProduct(string productId, UpdateProductRequest request);
        Task<BaseResponsePagingViewModel<ProductWithoutAttributeResponse>> GetAllProduct(PagingRequest paging);
        Task<BaseResponsePagingViewModel<ReportProductResponse>> GetReportProductCannotPrepare();
        Task<BaseResponseViewModel<ProductResponse>> GetProductByProductAttribute(string productAttributeId);
        Task<BaseResponseViewModel<dynamic>> UpdateProductCannotRepair(string productId, bool IsAvailable);
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
                var getAllProductInMenu = await _unitOfWork.Repository<ProductInMenu>().GetAll().ToListAsync();
                if (request.IsActive == false)
                {
                    foreach (var attribute in productAttribute)
                    {
                        var productsInMenus = getAllProductInMenu.Where(x => x.ProductId == attribute.Id);
                        if (attribute.IsActive == true)
                        { 
                            attribute.IsActive = false;
                            attribute.UpdateAt = DateTime.Now;
                        }
                        foreach (var productInMenu in productsInMenus)
                        {
                            if (productInMenu.Status == (int)ProductInMenuStatusEnum.Avaliable)
                            {
                                productInMenu.Status = (int)ProductInMenuStatusEnum.OutOfStock;
                                productInMenu.UpdatedAt = DateTime.Now;
                            }
                        }
                    }
                }
                else if (request.IsActive == true)
                {
                    foreach (var attribute in productAttribute)
                    {
                        var productsInMenus = getAllProductInMenu.Where(x => x.ProductId == attribute.Id);
                        if (attribute.IsActive == false)
                        {
                            attribute.IsActive = true;
                            attribute.UpdateAt = DateTime.Now;
                        }
                        foreach (var productInMenu in productsInMenus)
                        {
                            if (productInMenu.Status == (int)ProductInMenuStatusEnum.OutOfStock)
                            {
                                productInMenu.Status = (int)ProductInMenuStatusEnum.Avaliable;
                                productInMenu.UpdatedAt = DateTime.Now;
                            }
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

        public async Task<BaseResponsePagingViewModel<ProductWithoutAttributeResponse>> GetAllProduct(PagingRequest paging)
        {
            try
            {
                var products = _unitOfWork.Repository<Product>().GetAll()
                                .ProjectTo<ProductWithoutAttributeResponse>(_mapper.ConfigurationProvider)
                                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<ProductWithoutAttributeResponse>()
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
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<ReportProductResponse>> GetReportProductCannotPrepare()
        {
            try
            {
                List<ReportProduct> reportProducts = new List<ReportProduct>();

                var getAllStaff = await _unitOfWork.Repository<Staff>().GetAll().Where(x => x.StoreId != null).ToListAsync();
                var getAllTimeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll().ToListAsync();
                var getAllProduct = await _unitOfWork.Repository<Product>().GetAll().ToListAsync();
                var getAllStore = await _unitOfWork.Repository<Store>().GetAll().ToListAsync();

                foreach (var staff in getAllStaff)
                {
                    foreach (var timeSlot in getAllTimeSlot)
                    {
                        var key = RedisDbEnum.Staff.GetDisplayName() + ":" + staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                        var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);
                        if (redisValue.HasValue == true)
                        {
                            PackageStaffResponse packageResponse = JsonConvert.DeserializeObject<PackageStaffResponse>(redisValue);
                            if (packageResponse.ErrorProducts is not null)
                            {
                                foreach (var product in packageResponse.ErrorProducts)
                                {
                                    if (product.IsRefuse == true)
                                    {
                                        ReportProduct reportProduct = new ReportProduct()
                                        {
                                            ProductName = product.ProductName,
                                            StoreName = getAllStore.FirstOrDefault(x => x.Id == staff.StoreId).StoreName,
                                            ProductAttributeId = product.ProductId
                                        };
                                        reportProducts.Add(reportProduct);
                                    }
                                }
                            }
                        }
                    }
                }

                var reportProductResponse = reportProducts.GroupBy(store => store.StoreName)
                                                      .Select(group => new ReportProductResponse
                                                      {
                                                          StoreName = group.Key,
                                                          Products = group.Select(store => new ReportProduct
                                                          {
                                                              StoreName = store.StoreName,
                                                              ProductName = store.ProductName,
                                                              ProductAttributeId = store.ProductAttributeId
                                                          }).ToList()
                                                      }).ToList();

                return new BaseResponsePagingViewModel<ReportProductResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = reportProductResponse.Count()
                    },
                    Data = reportProductResponse
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<ProductResponse>> GetProductByProductAttribute(string productAttributeId)
        {
            try
            {
                var productAttribute = await _unitOfWork.Repository<ProductAttribute>().GetAll()
                  .FirstOrDefaultAsync(x => x.Id == Guid.Parse(productAttributeId));

                if (productAttribute == null)
                    throw new ErrorResponse(404, (int)ProductAttributeErrorEnums.NOT_FOUND,
                        ProductAttributeErrorEnums.NOT_FOUND.GetDisplayName());

                var product = await _unitOfWork.Repository<Product>().GetAll()
                  .FirstOrDefaultAsync(x => x.Id == productAttribute.ProductId);

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

        public async Task<BaseResponseViewModel<dynamic>> UpdateProductCannotRepair(string productId, bool IsAvailable)
        {
            try
            {
                var getAllProductAttribute = await _unitOfWork.Repository<ProductAttribute>().GetAll().ToListAsync();

                var productAttribute = getAllProductAttribute.FirstOrDefault(x => x.Id == Guid.Parse(productId));
                if(productAttribute == null)
                    throw new ErrorResponse(404, (int)ProductAttributeErrorEnums.NOT_FOUND,
                        ProductAttributeErrorEnums.NOT_FOUND.GetDisplayName());
                var productAttributes = getAllProductAttribute.Where(x => x.ProductId == productAttribute.ProductId);
                
                var getAllTimeslot = await _unitOfWork.Repository<TimeSlot>().GetAll().ToListAsync();
                var getAllProductInMenu = await _unitOfWork.Repository<ProductInMenu>().GetAll().ToListAsync();
                if (IsAvailable == false)
                {
                    foreach (var attribute in productAttributes)
                    {
                        var productsInMenus = getAllProductInMenu.Where(x => x.ProductId == attribute.Id);
                        foreach (var productInMenu in productsInMenus)
                        {
                            if (productInMenu.Status == (int)ProductInMenuStatusEnum.Avaliable)
                            {
                                productInMenu.Status = (int)ProductInMenuStatusEnum.OutOfStock;
                                productInMenu.UpdatedAt = DateTime.Now;
                            }
                        }
                    }
                }
                else if (IsAvailable == true)
                {
                    foreach (var attribute in productAttributes)
                    {
                        var productsInMenus = getAllProductInMenu.Where(x => x.ProductId == attribute.Id);
                        foreach (var productInMenu in productsInMenus)
                        {
                            if (productInMenu.Status == (int)ProductInMenuStatusEnum.OutOfStock)
                            {
                                productInMenu.Status = (int)ProductInMenuStatusEnum.Avaliable;
                                productInMenu.UpdatedAt = DateTime.Now;
                            }
                        }
                        foreach (var timeSlot in getAllTimeslot)
                        {
                            var keyStaff = RedisDbEnum.Staff.GetDisplayName() + ":" + attribute.Product.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");                           
                            var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyStaff, null);
                            if (redisValue.HasValue == true)
                            {
                                PackageStaffResponse packageStaff = JsonConvert.DeserializeObject<PackageStaffResponse>(redisValue);

                                var productError = packageStaff.ErrorProducts.FirstOrDefault(x => x.ProductId == attribute.Id && x.IsRefuse == true);
                                if (productError != null)
                                {
                                    //packageStaff.ErrorProducts.Remove(productError);
                                    productError.IsRefuse = false;
                                    ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyStaff, packageStaff);
                                }
                                
                            }
                        }
                    }                
                }
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<dynamic>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}