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
    }
}