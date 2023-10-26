using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    #region staff
    public class PackageStaffResponse
    {
        public int TotalProductInDay { get; set; }
        public int TotalProductPending { get; set; }
        public int TotalProductReady { get; set; }
        public int TotalProductError { get; set; }
        public List<ProductTotalDetail> ProductTotalDetails { get; set; }
        public List<ErrorProduct> ErrorProducts { get; set; }
        public List<PackageStationResponse> PackageStations { get; set; }
    }
    public class PackageStationResponse
    {
        public Guid StationId { get; set; }
        public string StationName { get; set; } = null!;
        public int TotalQuantity { get; set; }
        public int ReadyQuantity { get; set; }
        public bool IsShipperAssign { get; set; }
        public List<PackageDetailResponse> PackageStationDetails { get; set; }
        public List<PackageDetailResponse> ListPackageMissing { get; set; }
        public HashSet<KeyValuePair<Guid, string>> ListOrder { get; set; }
    }
    public class PackageDetailResponse
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public List<BoxProduct> BoxProducts { get; set; }
    }
    public class BoxProduct
    {
        public Guid BoxId { get; set; }
        public string BoxCode { get; set; }
        public int Quantity { get; set; }
    }
    public class ErrorProduct
    {
        public Guid ProductId { get; set; }
        public Guid ProductInMenuId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public int ReConfirmQuantity { get; set; }
        public Guid? StationId { get; set; }
        public List<Guid>? ListBox { get; set; }
        public int ReportMemType { get; set; }
        public bool IsRefuse { get; set; }
    }
    public class ProductTotalDetail
    {
        public Guid ProductId { get; set; }
        public Guid ProductInMenuId { get; set; }
        public string ProductName { get; set; }
        public int PendingQuantity { get; set; }
        public int ReadyQuantity { get; set; }
        public int ErrorQuantity { get; set; }
        //số product chờ. Ví dụ: order nhỏ có 3 mà sau khi cập nhật hết 1 vòng líst,
        //số lượng xác nhận còn lẻ 1 => không đủ để cập nhật order 3 => để lẻ 1 ở waiting
        public int WaitingQuantity { get; set; }
        public List<ProductDetail> ProductDetails { get; set; }
    }
    public class ProductDetail
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; }
        public Guid StationId { get; set; }
        public DateTime CheckInDate { get; set; }
        public int QuantityOfProduct { get; set; }
        public int ErrorQuantity { get; set; }
        public bool IsFinishPrepare { get; set; }
        public bool IsAssignToShipper { get; set; }
    }
    #endregion

    #region shipper
    public class PackageShipperResponse
    {
        public List<PackStationDetailGroupByBox> PackStationDetailGroupByBoxes { get; set; }

        public List<PackageStoreShipperResponse> PackageStoreShipperResponses { get; set; }
    }
    public class PackStationDetailGroupByBox
    {
        public Guid BoxId { get; set; }
        public string BoxCode { get; set; }
        public bool IsInBox { get; set; }
        public List<PackageDetailResponse> ListProduct { get; set; }
    }
    public class PackageStoreShipperResponse
    {
        public Guid StoreId { get; set; }
        public string StoreName { get; set; }
        public int TotalQuantity { get; set; }
        public bool IsTaken { get; set; }
        public bool IsInBox { get; set; } = false;
        public List<PackStationDetailGroupByProduct> PackStationDetailGroupByProducts { get; set; }

        //chỉ được thêm vào khi toàn bộ product trong order đc confirm
        public List<Guid> ListOrderId { get; set; }
    }

    public class PackStationDetailGroupByProduct
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int TotalQuantity { get; set; }
        public List<BoxProduct> BoxProducts { get; set; }
    }
    #endregion

    #region Order operation
    public class PackageOrderResponse
    {
        public int TotalConfirm { get; set; }
        public int NumberHasConfirm { get; set; }
        public List<PackageOrderBoxModel> PackageOrderBoxes { get; set; }
    }
    public class PackageOrderBoxModel
    {
        public Guid BoxId { get; set; }
        public string BoxCode { get; set; }
        public List<PackageOrderDetailModel> PackageOrderDetailModels { get; set; }
    }
    public class PackageOrderDetailModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public Guid ProductInMenuId { get; set; }
        public int Quantity { get; set; }
        public bool IsInBox { get; set; }
    }
    #endregion
}

