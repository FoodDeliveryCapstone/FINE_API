using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IImportService
    {
        Task<BaseResponseViewModel<ImportResponse>> ImportProductsByExcel(IFormFile excelFile);
    }

    public class ImportService : IImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public ImportService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<ImportResponse>> ImportProductsByExcel(IFormFile excelFile)
        {
            try
            {
                var getAllStore = await _unitOfWork.Repository<Store>().GetAll().ToListAsync();
                var getAllCategory = await _unitOfWork.Repository<Category>().GetAll().ToListAsync();
                var getAllProduct = await _unitOfWork.Repository<Product>().GetAll().ToListAsync();

                var importResponse = new ImportResponse
                {
                    ErrorLine = new List<int>()
                };

                //read excel file
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(excelFile.OpenReadStream()))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    int row = 4; // Starting from row 4

                    while (worksheet.Cells[row, 1].Value != null)
                    {
                        string storeName = worksheet.Cells[row, 1].Value?.ToString();
                        string categoryName = worksheet.Cells[row, 2].Value?.ToString();
                        var productCode = worksheet.Cells[row, 3].Value?.ToString();

                        var store = getAllStore.FirstOrDefault(s => s.StoreName == storeName);
                        if(store == null)
                        {
                            //write error line and move to next row
                            importResponse.ErrorLine.Add(row);
                            row++;
                            continue;
                        }
                        var category = getAllCategory.FirstOrDefault(c => c.Name == categoryName);
                        if(category == null)
                        {
                            //write error line and move to next row
                            importResponse.ErrorLine.Add(row);
                            row++;
                            continue;
                        }

                        var checkProductCode = getAllProduct.FirstOrDefault(x => x.ProductCode == productCode);
                        if (checkProductCode != null)
                        {
                            //write error line and move to next row
                            importResponse.ErrorLine.Add(row);
                            row++;
                            continue;
                        }

                        string isStackableValue = worksheet.Cells[row, 6].Value?.ToString();
                        bool isStackable = isStackableValue.ToLower() == "true";

                        Product product = new Product
                        {
                            Id = Guid.NewGuid(),
                            StoreId = store.Id,
                            CategoryId = category.Id,
                            ProductCode = productCode,
                            ProductName = worksheet.Cells[row, 4].Value?.ToString(),
                            ProductType = int.Parse(worksheet.Cells[row, 5].Value?.ToString()),
                            IsStackable = isStackable,
                            ImageUrl = worksheet.Cells[row, 7].Value?.ToString(),
                            CreateAt = DateTime.Now,
                        };
                        _unitOfWork.Repository<Product>().InsertAsync(product);
                        _unitOfWork.CommitAsync();
                        row++;
                    }
                }            

                return new BaseResponseViewModel<ImportResponse>
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = importResponse
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
