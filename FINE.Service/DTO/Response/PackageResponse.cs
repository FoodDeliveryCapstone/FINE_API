using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class PackageResponse
    {
        public int TotalProductInDay { get; set; }
        public int TotalProductPending { get; set; }
        public int TotalProductReady { get; set; }
        public int TotalProductError { get; set; }
        public List<ProductTotalDetail> ProductTotalDetails { get; set; }
        public List<ErrorProduct> ErrorProducts { get; set; }
        public List<PackageStationResponse> PackageStations { get; set; }
    }
    public class ProductTotalDetail
    {
        public Guid ProductId { get; set; }
        public Guid ProductInMenuId { get; set; }
        public string ProductName { get; set; }
        public int PendingQuantity { get; set; }
        public int ReadyQuantity { get; set; }
        public int ErrorQuantity { get; set; }
        public int WaitingQuantity { get; set; }
        public List<ProductDetail> ProductDetails { get; set; }
    }
    public class ProductDetail
    {
        public Guid OrderId { get; set; }
        public Guid StationId { get; set; }
        public Guid BoxId { get; set; }
        public DateTime CheckInDate { get; set; }
        public int Quantity { get; set; }
        public bool IsReady { get; set; }
    }

    public class ErrorProduct
    {
        public Guid ProductId { get; set; }
        public Guid ProductInMenuId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public Guid? StationId { get; set; }
        public int ReportMemType { get; set; }
    }

    public class PackageStatusResponse
    {
        public Guid ProductId { get; set; }
        public Guid ProductInMenuId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }
    public class PackageStationResponse
    {
        public Guid StationId { get; set; }
        public string StationName { get; set; } = null!;
        public int TotalQuantity { get; set; }
        public int ReadyQuantity { get; set; }
        public bool IsShipperAssign { get; set; }
        public List<PackageStationDetailResponse> PackageStationDetails { get; set; }
        public List<PackageStationDetailResponse> ListPackageMissing { get; set; }

    }

    public class PackageStationDetailResponse
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }

    public class PackageOrderDetailModel
    {
        public Guid ProductId { get; set; }
        public Guid ProductInMenuId { get; set; }
        public int Quantity { get; set; }
        public int ErrorQuantity { get; set; }
        public bool IsReady { get; set; }
    }

    public class PackageShipperResponse
    {
        public Guid StoreId { get; set; }
        public string StoreName { get; set; }
        public List<PackageShipperDetailResponse> PackageShipperDetails { get; set; }
    }

    public class PackageShipperDetailResponse
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }
}

