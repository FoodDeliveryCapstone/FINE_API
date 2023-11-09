using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Response;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.Service
{
    public interface IImportService
    {
        Task<BaseResponseViewModel<dynamic>> ImportProductsByExcel(string excelPath);
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

        public async Task<BaseResponseViewModel<dynamic>> ImportProductsByExcel(string excelPath)
        {
            try
            {
                var getAllStore = await _unitOfWork.Repository<Store>().GetAll().ToListAsync();
                var getAllCategory = await _unitOfWork.Repository<Category>().GetAll().ToListAsync();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                FileInfo file = new FileInfo(excelPath);
                using (ExcelPackage package = new ExcelPackage(file))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    int row = 4; // Starting from row 4

                    while (worksheet.Cells[row, 1].Value != null)
                    {
                        string storeName = worksheet.Cells[row, 1].Value?.ToString();
                        string categoryName = worksheet.Cells[row, 2].Value?.ToString();

                        var storeId = getAllStore.FirstOrDefault(s => s.StoreName == storeName).Id;
                        var categoryId = getAllCategory.FirstOrDefault(c => c.Name == categoryName).Id;

                        string isStackableValue = worksheet.Cells[row, 6].Value?.ToString();
                        bool isStackable = isStackableValue.ToLower() == "true";

                        Product product = new Product
                        {
                            Id = Guid.NewGuid(),
                            StoreId = storeId,
                            CategoryId = categoryId,
                            ProductCode = worksheet.Cells[row, 3].Value?.ToString(),
                            ProductName = worksheet.Cells[row, 4].Value?.ToString(),
                            ProductType = int.Parse(worksheet.Cells[row, 5].Value?.ToString()),
                            IsStackable = isStackable,
                            ImageUrl = worksheet.Cells[row, 7].Value?.ToString(),
                            CreateAt = DateTime.Now,
                        };
                        await _unitOfWork.Repository<Product>().InsertAsync(product);
                        row++;
                    }
                }
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<dynamic>
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
