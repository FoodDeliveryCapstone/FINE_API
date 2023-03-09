﻿using System.Linq.Dynamic.Core;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Algorithm;
using NTQ.Sdk.Core.Utilities;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IProductService
    {
        Task<BaseResponsePagingViewModel<ProductResponse>> GetProducts(ProductResponse request, PagingRequest paging);
        Task<BaseResponseViewModel<ProductResponse>> GetProductById(int productId);
        Task<BaseResponseViewModel<ProductResponse>> GetProductByCode(string code);
        Task<BaseResponsePagingViewModel<ProductResponse>> GetProductByStore(int storeId, PagingRequest paging);
        Task<BaseResponsePagingViewModel<ProductResponse>> GetProductByCategory(int cateId, PagingRequest paging);
        Task<BaseResponsePagingViewModel<ProductResponse>> GetProductByMenu(int menuId, PagingRequest paging);
        Task<BaseResponseViewModel<ProductResponse>> CreateProduct(CreateProductRequest request);
        Task<BaseResponseViewModel<ProductResponse>> UpdateProduct(int productId, UpdateProductRequest request);
    }

    public class ProductService : IProductService
    {
        private readonly FineStgDbContext _context;
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private IAddProductToMenuService _addProductToMenuService;

        
       
        public ProductService(FineStgDbContext context,IMapper mapper, IUnitOfWork unitOfWork, IAddProductToMenuService addProductToMenuService)
        {
            _context = context;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _addProductToMenuService = addProductToMenuService;
        }

        public async Task<BaseResponseViewModel<ProductResponse>> CreateProduct(CreateProductRequest request)
        {
            var checkProduct = _unitOfWork.Repository<Product>().Find(x => x.ProductCode == request.ProductCode);
            if (checkProduct != null)
                throw new ErrorResponse(404, (int)ProductErrorEnums.PRODUCT_CODE_EXSIST,
                    ProductErrorEnums.PRODUCT_CODE_EXSIST.GetDisplayName());

            var product = _mapper.Map<CreateProductRequest, Product>(request);

            product.CreateAt = DateTime.Now;

            await _unitOfWork.Repository<Product>().InsertAsync(product);
            await _unitOfWork.CommitAsync();

            if (request.extraProducts != null)
            {
                var genProduct = _unitOfWork.Repository<Product>().Find(x => x.ProductCode == product.ProductCode);
                foreach (var extraProduct in request.extraProducts)
                {
                    var productExtra = new Product()
                    {
                        GeneralProductId = genProduct.Id,
                        ProductCode = genProduct.ProductCode + '_' + extraProduct.Size,
                        ProductName = genProduct.ProductCode + " (" + extraProduct.Size + ')',
                        CategoryId = genProduct.CategoryId,
                        StoreId = genProduct.StoreId,
                        SizePrice = extraProduct.SizePrice,
                        Size = extraProduct.Size,
                        CreateAt = DateTime.Now,
                        IsActive = true,
                    };

                    await _unitOfWork.Repository<Product>().InsertAsync(productExtra);
                    await _unitOfWork.CommitAsync();
                }

            }

            
            //Add Product to Menu 
            if (request.addProductToMenu != null)
            {

                var genProduct = _unitOfWork.Repository<Product>().Find(x => x.ProductCode == product.ProductCode);
                var addProductToMenu = request.addProductToMenu.FirstOrDefault();
                if (addProductToMenu.ProductId == null)
                {
                    addProductToMenu.ProductId = genProduct.Id;
                }
                await _addProductToMenuService.AddProductIntoMenu(addProductToMenu);
            }

            return new BaseResponseViewModel<ProductResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                }
            };
        }

        public async Task<BaseResponsePagingViewModel<ProductResponse>> GetProductByCategory(int cateId,
            PagingRequest paging)
        {
            var products = _unitOfWork.Repository<Product>().GetAll()
                .Where(x => x.CategoryId == cateId)
                .ProjectTo<ProductResponse>(_mapper.ConfigurationProvider)
                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

            return new BaseResponsePagingViewModel<ProductResponse>()
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

        public async Task<BaseResponseViewModel<ProductResponse>> GetProductByCode(string code)
        {
            var product = _unitOfWork.Repository<Product>().GetAll()
                .FirstOrDefault(x => x.ProductCode == code);

            if (product == null)
                throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND_CODE,
                    ProductErrorEnums.NOT_FOUND_CODE.GetDisplayName());

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

        public async Task<BaseResponseViewModel<ProductResponse>> GetProductById(int productId)
        {
            var product = _unitOfWork.Repository<Product>().GetAll()
                .FirstOrDefault(x => x.Id == productId);

            if (product == null)
                throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND_ID,
                    ProductErrorEnums.NOT_FOUND_ID.GetDisplayName());

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

        public async Task<BaseResponsePagingViewModel<ProductResponse>> GetProductByStore(int storeId,
            PagingRequest paging)
        {
            var products = _unitOfWork.Repository<Product>().GetAll()
                .Where(x => x.StoreId == storeId)
                .ProjectTo<ProductResponse>(_mapper.ConfigurationProvider)
                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

            return new BaseResponsePagingViewModel<ProductResponse>()
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

        public async Task<BaseResponsePagingViewModel<ProductResponse>> GetProducts(ProductResponse filter,
            PagingRequest paging)
        {
            var product = _unitOfWork.Repository<Product>().GetAll()
                .ProjectTo<ProductResponse>(_mapper.ConfigurationProvider)
                .DynamicFilter(filter)
                .DynamicSort(filter)
                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                    Constants.DefaultPaging);


            return new BaseResponsePagingViewModel<ProductResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = product.Item1
                },
                Data = product.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<ProductResponse>> UpdateProduct(int ProductId,
            UpdateProductRequest request)
        {
            //update general product
            var product = _unitOfWork.Repository<Product>().GetAll()
                .FirstOrDefault(x => x.Id == ProductId);

            if (product == null)
                throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND_ID,
                    ProductErrorEnums.NOT_FOUND_ID.GetDisplayName());

            var checkProductCode = _unitOfWork.Repository<Product>()
                .GetWhere(x => x.Id != ProductId && x.ProductCode.Contains(request.ProductCode));
            if (checkProductCode != null)
                throw new ErrorResponse(404, (int)ProductErrorEnums.PRODUCT_CODE_EXSIST,
                    ProductErrorEnums.PRODUCT_CODE_EXSIST.GetDisplayName());

            var updateProduct = _mapper.Map<UpdateProductRequest, Product>(request, product);

            updateProduct.UpdateAt = DateTime.Now;

            await _unitOfWork.Repository<Product>().UpdateDetached(updateProduct);
            await _unitOfWork.CommitAsync();
            //update product extra (nếu có)
            if (request.extraProduct != null)
            {
                var extraProduct = _unitOfWork.Repository<Product>().GetAll()
                    .Where(x => x.GeneralProductId == ProductId)
                    .ToList();
                //ban đầu sản phẩm không có product extra -> create
                if (extraProduct == null)
                {
                    foreach (var item in request.extraProduct)
                    {
                        var newProductExtra = _mapper.Map<UpdateProductExtraRequest, CreateExtraProductRequest>(item);
                        CreateExtraProduct(ProductId, newProductExtra);
                    }
                }

                // ban đầu sản phẩm có product extra 
                foreach (var item in request.extraProduct)
                {
                    // ktra request đã từng được create hay chưa (chưa có id là chưa từng đc create)
                    if (item.Id == null)
                    {
                        var newProductExtra = _mapper.Map<UpdateProductExtraRequest, CreateExtraProductRequest>(item);
                        CreateExtraProduct(ProductId, newProductExtra);
                    }

                    //đã từng được create thì kiếm id đó trong list có sẵn -> update
                    var extraProductUpdate = extraProduct.Find(x => x.Id == item.Id);
                    var updateProductExtra = _mapper.Map<UpdateProductRequest, Product>(request, extraProductUpdate);

                    updateProductExtra.ProductCode = request.ProductCode + '_' + item.Size;
                    updateProductExtra.ProductName = request.ProductCode + " (" + item.Size + ')';
                    updateProductExtra.CategoryId = request.CategoryId;
                    updateProductExtra.SizePrice = item.SizePrice;
                    updateProductExtra.Size = item.Size;
                    updateProductExtra.UpdateAt = DateTime.Now;
                    updateProductExtra.IsActive = request.IsActive;

                    await _unitOfWork.Repository<Product>().UpdateDetached(extraProductUpdate);
                    await _unitOfWork.CommitAsync();
                }
            }

            return new BaseResponseViewModel<ProductResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                }
            };
        }

        public async void CreateExtraProduct(int genProductId, CreateExtraProductRequest extraProduct)
        {
            var genProduct = _unitOfWork.Repository<Product>().GetAll()
                .FirstOrDefault(x => x.GeneralProductId == genProductId);
            var productExtra = new Product()
            {
                GeneralProductId = genProduct.Id,
                ProductCode = genProduct.ProductCode + '_' + extraProduct.Size,
                ProductName = genProduct.ProductCode + " (" + extraProduct.Size + ')',
                CategoryId = genProduct.CategoryId,
                StoreId = genProduct.StoreId,
                SizePrice = extraProduct.SizePrice,
                Size = extraProduct.Size,
                CreateAt = DateTime.Now,
                IsActive = true,
            };
            await _unitOfWork.Repository<Product>().InsertAsync(productExtra);
            await _unitOfWork.CommitAsync();
        }

        public async Task<BaseResponsePagingViewModel<ProductResponse>> GetProductByMenu(int menuId, PagingRequest paging)
        {
            try
            {
                #region check floor and area exist
                var checkMenu = _unitOfWork.Repository<Menu>().GetAll()
                              .FirstOrDefault(x => x.Id == menuId);
                if (checkMenu == null)
                    throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND_ID,
                        MenuErrorEnums.NOT_FOUND_ID.GetDisplayName());
                #endregion

                var product = _unitOfWork.Repository<Product>().GetAll()

                  .Include(x => x.ProductInMenus)
                  .ThenInclude(x => x.Menu)
                 .Where(x => x.ProductInMenus.Any(x => x.Menu.Id == menuId))

                 .ProjectTo<ProductResponse>(_mapper.ConfigurationProvider)
                 .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<ProductResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = product.Item1
                    },
                    Data = product.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}