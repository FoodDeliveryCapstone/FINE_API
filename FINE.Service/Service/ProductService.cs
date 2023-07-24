//using System.Data;
//using System.Linq.Dynamic.Core;
//using System.Runtime.CompilerServices;
//using AutoMapper;
//using AutoMapper.QueryableExtensions;
//using Azure.Core;
//using FINE.Data.Entity;
//using FINE.Data.UnitOfWork;
//using FINE.Service.Attributes;
//using FINE.Service.DTO.Request;
//using FINE.Service.DTO.Request.Product;
//using FINE.Service.DTO.Request.ProductInMenu;
//using FINE.Service.DTO.Response;
//using FINE.Service.Exceptions;
//using FINE.Service.Utilities;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NetTopologySuite.Algorithm;
//using Newtonsoft.Json;
//using static FINE.Service.Helpers.ErrorEnum;

//namespace FINE.Service.Service
//{
//    public interface IProductService
//    {
//        Task<BaseResponsePagingViewModel<ProductResponse>> GetProducts(ProductResponse request, PagingRequest paging);
//        Task<BaseResponseViewModel<ProductResponse>> GetProductById(int productId);
//        Task<BaseResponseViewModel<ProductResponse>> GetProductByCode(string code);
//        Task<BaseResponsePagingViewModel<ProductResponse>> GetProductByStore(int storeId, PagingRequest paging);
//        Task<BaseResponsePagingViewModel<ProductResponse>> GetProductByCategory(int cateId, PagingRequest paging);
//        Task<BaseResponsePagingViewModel<ProductInMenuResponse>> GetProductByMenu(int menuId, PagingRequest paging);
//        Task<BaseResponseViewModel<ProductResponse>> CreateProduct(CreateProductRequest request);
//        Task<BaseResponseViewModel<ProductResponse>> UpdateProduct(int productId, UpdateProductRequest request);
//        void GetProductFromPassioDB();
//    }

//    public class ProductService : IProductService
//    {
//        private IMapper _mapper;
//        private readonly IUnitOfWork _unitOfWork;

//        public ProductService(IMapper mapper, IUnitOfWork unitOfWork)
//        {
//            _mapper = mapper;
//            _unitOfWork = unitOfWork;
//        }

//        public async Task<BaseResponseViewModel<ProductResponse>> CreateProduct(CreateProductRequest request)
//        {
//            try
//            {
//                var checkProduct = _unitOfWork.Repository<Product>().Find(x => x.ProductCode == request.ProductCode);
//                if (checkProduct != null)
//                    throw new ErrorResponse(404, (int)ProductErrorEnums.PRODUCT_CODE_EXSIST,
//                        ProductErrorEnums.PRODUCT_CODE_EXSIST.GetDisplayName());

//                var product = _mapper.Map<CreateProductRequest, Product>(request);

//                product.CreateAt = DateTime.Now;

//                if (request.extraProducts != null)
//                {
//                    foreach (var extraProduct in request.extraProducts)
//                    {
//                        var productExtra = new Product()
//                        {
//                            ProductCode = product.ProductCode + '_' + extraProduct.Size,
//                            ProductName = product.ProductCode + " (" + extraProduct.Size + ')',
//                            CategoryId = product.CategoryId,
//                            StoreId = product.StoreId,
//                            SizePrice = extraProduct.SizePrice,
//                            Size = extraProduct.Size,
//                            CreateAt = DateTime.Now,
//                            IsActive = true,
//                        };
//                        product.InverseGeneralProduct.Add(productExtra);
//                    }
//                }
//                await _unitOfWork.Repository<Product>().InsertAsync(product);
//                await _unitOfWork.CommitAsync();

//                return new BaseResponseViewModel<ProductResponse>()
//                {
//                    Status = new StatusViewModel()
//                    {
//                        Message = "Success",
//                        Success = true,
//                        ErrorCode = 0
//                    }
//                };
//            }
//            catch (ErrorResponse ex)
//            {
//                throw;
//            }
//        }

//        public async Task<BaseResponsePagingViewModel<ProductResponse>> GetProductByCategory(int cateId,
//            PagingRequest paging)
//        {
//            var products = _unitOfWork.Repository<Product>().GetAll()
//                .Where(x => x.CategoryId == cateId)
//                .ProjectTo<ProductResponse>(_mapper.ConfigurationProvider)
//                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

//            return new BaseResponsePagingViewModel<ProductResponse>()
//            {
//                Metadata = new PagingsMetadata()
//                {
//                    Page = paging.Page,
//                    Size = paging.PageSize,
//                    Total = products.Item1
//                },
//                Data = products.Item2.ToList()
//            };
//        }

//        public async Task<BaseResponseViewModel<ProductResponse>> GetProductByCode(string code)
//        {
//            var product = _unitOfWork.Repository<Product>().GetAll()
//                .FirstOrDefault(x => x.ProductCode == code);

//            if (product == null)
//                throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND_CODE,
//                    ProductErrorEnums.NOT_FOUND_CODE.GetDisplayName());

//            return new BaseResponseViewModel<ProductResponse>()
//            {
//                Status = new StatusViewModel()
//                {
//                    Message = "Success",
//                    Success = true,
//                    ErrorCode = 0
//                },
//                Data = _mapper.Map<ProductResponse>(product)
//            };
//        }

//        public async Task<BaseResponseViewModel<ProductResponse>> GetProductById(int productId)
//        {
//            var product = _unitOfWork.Repository<Product>().GetAll()
//                .Include(x => x.ProductInMenus)

//                .FirstOrDefault(x => x.Id == productId);

//            if (product == null)
//                throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND,
//                    ProductErrorEnums.NOT_FOUND.GetDisplayName());

//            return new BaseResponseViewModel<ProductResponse>()
//            {
//                Status = new StatusViewModel()
//                {
//                    Message = "Success",
//                    Success = true,
//                    ErrorCode = 0
//                },
//                Data = _mapper.Map<ProductResponse>(product)
//            };
//        }

//        public async Task<BaseResponsePagingViewModel<ProductResponse>> GetProductByStore(int storeId,
//            PagingRequest paging)
//        {
//            var products = _unitOfWork.Repository<Product>().GetAll()
//                .Where(x => x.StoreId == storeId)
//                .ProjectTo<ProductResponse>(_mapper.ConfigurationProvider)
//                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

//            return new BaseResponsePagingViewModel<ProductResponse>()
//            {
//                Metadata = new PagingsMetadata()
//                {
//                    Page = paging.Page,
//                    Size = paging.PageSize,
//                    Total = products.Item1
//                },
//                Data = products.Item2.ToList()
//            };
//        }

//        public async Task<BaseResponsePagingViewModel<ProductResponse>> GetProducts(ProductResponse filter,
//            PagingRequest paging)
//        {
//            var product = _unitOfWork.Repository<Product>().GetAll()
//                                            .ProjectTo<ProductResponse>(_mapper.ConfigurationProvider)
//                                            .DynamicFilter(filter)
//                                            .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
//                                                Constants.DefaultPaging);


//            return new BaseResponsePagingViewModel<ProductResponse>()
//            {
//                Metadata = new PagingsMetadata()
//                {
//                    Page = paging.Page,
//                    Size = paging.PageSize,
//                    Total = product.Item1
//                },
//                Data = product.Item2.ToList()
//            };
//        }

//        public async Task<BaseResponseViewModel<ProductResponse>> UpdateProduct(int productId,
//            UpdateProductRequest request)
//        {
//            try
//            {
//                //update general passioProduct
//                var product = _unitOfWork.Repository<Product>().GetAll()
//                    .FirstOrDefault(x => x.Id == productId);

//                if (product == null)
//                    throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND,
//                        ProductErrorEnums.NOT_FOUND.GetDisplayName());

//                var checkProductCode = _unitOfWork.Repository<Product>()
//                       .Find(x => x.Id != productId && x.ProductCode == request.ProductCode);
//                if (checkProductCode != null)
//                    throw new ErrorResponse(404, (int)ProductErrorEnums.PRODUCT_CODE_EXSIST,
//                        ProductErrorEnums.PRODUCT_CODE_EXSIST.GetDisplayName());

//                var updateProduct = _mapper.Map<UpdateProductRequest, Product>(request, product);

//                updateProduct.UpdateAt = DateTime.Now;

//                await _unitOfWork.Repository<Product>().UpdateDetached(updateProduct);
//                await _unitOfWork.CommitAsync();
//                //update passioProduct extra (nếu có)
//                if (request.extraProducts != null)
//                {
//                    var extraProduct = _unitOfWork.Repository<Product>().GetAll()
//                        .Where(x => x.GeneralProductId == productId)
//                        .ToList();
//                    //ban đầu sản phẩm không có passioProduct extra -> create
//                    if (extraProduct == null)
//                    {
//                        foreach (var item in request.extraProducts)
//                        {
//                            var newProductExtra = _mapper.Map<UpdateProductExtraRequest, CreateExtraProductRequest>(item);
//                            CreateExtraProduct(productId, newProductExtra);
//                        }
//                    }

//                    // ban đầu sản phẩm có passioProduct extra 
//                    foreach (var item in request.extraProducts)
//                    {
//                        // ktra request đã từng được create hay chưa (chưa có id là chưa từng đc create)
//                        if (item.Id == null)
//                        {
//                            var newProductExtra = _mapper.Map<UpdateProductExtraRequest, CreateExtraProductRequest>(item);
//                            CreateExtraProduct(productId, newProductExtra);
//                        }

//                        //đã từng được create thì kiếm id đó trong list có sẵn -> update
//                        var extraProductUpdate = extraProduct.Find(x => x.Id == item.Id);
//                        var updateProductExtra = _mapper.Map<UpdateProductRequest, Product>(request, extraProductUpdate);

//                        updateProductExtra.ProductCode = request.ProductCode + '_' + item.Size;
//                        updateProductExtra.ProductName = request.ProductCode + " (" + item.Size + ')';
//                        updateProductExtra.CategoryId = request.CategoryId;
//                        updateProductExtra.SizePrice = item.SizePrice;
//                        updateProductExtra.Size = item.Size;
//                        updateProductExtra.UpdateAt = DateTime.Now;
//                        updateProductExtra.IsActive = request.IsActive;

//                        await _unitOfWork.Repository<Product>().UpdateDetached(extraProductUpdate);
//                        await _unitOfWork.CommitAsync();
//                    }
//                }

//                return new BaseResponseViewModel<ProductResponse>()
//                {
//                    Status = new StatusViewModel()
//                    {
//                        Message = "Success",
//                        Success = true,
//                        ErrorCode = 0
//                    }
//                };
//            }
//            catch (ErrorResponse ex)
//            {
//                throw;
//            }
//        }

//        public async void CreateExtraProduct(int genProductId, CreateExtraProductRequest extraProduct)
//        {
//            var genProduct = _unitOfWork.Repository<Product>().GetAll()
//                .FirstOrDefault(x => x.GeneralProductId == genProductId);
//            var productExtra = new Product()
//            {
//                GeneralProductId = genProduct.Id,
//                ProductCode = genProduct.ProductCode + '_' + extraProduct.Size,
//                ProductName = genProduct.ProductCode + " (" + extraProduct.Size + ')',
//                CategoryId = genProduct.CategoryId,
//                StoreId = genProduct.StoreId,
//                SizePrice = extraProduct.SizePrice,
//                Size = extraProduct.Size,
//                CreateAt = DateTime.Now,
//                IsActive = true,
//            };
//            await _unitOfWork.Repository<Product>().InsertAsync(productExtra);
//            await _unitOfWork.CommitAsync();
//        }

//        public async Task<BaseResponsePagingViewModel<ProductInMenuResponse>> GetProductByMenu(int menuId, PagingRequest paging)
//        {
//            try
//            {
//                #region menu exsist
//                var checkMenu = _unitOfWork.Repository<Menu>().GetAll()
//                              .FirstOrDefault(x => x.Id == menuId);
//                if (checkMenu == null)
//                    throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND,
//                        MenuErrorEnums.NOT_FOUND.GetDisplayName());
//                #endregion

//                var product = _unitOfWork.Repository<ProductInMenu>().GetAll()

//                 .Where(x => x.MenuId == menuId)

//                 .ProjectTo<ProductInMenuResponse>(_mapper.ConfigurationProvider)
//                 .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

//                return new BaseResponsePagingViewModel<ProductInMenuResponse>()
//                {
//                    Metadata = new PagingsMetadata()
//                    {
//                        Page = paging.Page,
//                        Size = paging.PageSize,
//                        Total = product.Item1
//                    },
//                    Data = product.Item2.ToList()
//                };
//            }
//            catch (Exception ex)
//            {
//                throw;
//            }
//        }

//        public async void GetProductFromPassioDB()
//        {
//            try
//            {
//                string ConnStr = "Data Source=54.251.108.33;Database=ProdPassio;User Id=admin_passio;Password=vA+Q!N3pZdJyXuerBx9bCF;MultipleActiveResultSets=true;Integrated Security=true;Trusted_Connection=False;Encrypt=True;TrustServerCertificate=True";
//                SqlConnection Conn = new SqlConnection(ConnStr);

//                string SqlString = "select * from [Product] where Active = 1 and IsAvailable = 1 and PicURL <> '';";

//                SqlDataAdapter sda = new SqlDataAdapter(SqlString, Conn);
//                DataTable dt = new DataTable();
//                Conn.Open();
//                sda.Fill(dt);

//                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"Configuration\" + "listProductPassio.json");
//                using (StreamWriter sw = File.CreateText(path))
//                {
//                    var json = JsonConvert.SerializeObject(dt);
//                    sw.WriteLine(json);
//                }
//                Conn.Close();

//                using (StreamReader reader = File.OpenText(path))
//                {
//                    string json = reader.ReadToEnd();
//                    List<CreateProductPassio> lst = JsonConvert.DeserializeObject<List<CreateProductPassio>>(json);

//                    foreach(var passioProduct in lst)
//                    {
//                        var newProduct = new CreateProductRequest()
//                        {
//                            ProductCode = passioProduct.Code,
//                            ProductName = passioProduct.ProductName,
//                            CategoryId = 4,
//                            StoreId = 2,
//                            BasePrice = passioProduct.Price,
//                            ImageUrl = passioProduct.PicURL
//                        };
//                        var product = _mapper.Map<CreateProductRequest, Product>(newProduct);
//                        product.CreateAt = DateTime.Now;
//                        _unitOfWork.Repository<Product>().Insert(product);
//                        _unitOfWork.Commit();
//                    }
                     
//                }

//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//        }


//    }
//}