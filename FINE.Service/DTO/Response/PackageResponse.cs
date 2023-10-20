﻿using System;
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
        public Guid BoxId { get; set; }
        public DateTime CheckInDate { get; set; }
        public int Quantity { get; set; }
        public bool IsFinishPrepare { get; set; }
        public bool IsAssignToShipper { get; set; }
    }

    public class ErrorProduct
    {
        public Guid ProductId { get; set; }
        public Guid ProductInMenuId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public int ReConfirmQuantity { get; set; }
        public Guid? StationId { get; set; }
        public int ReportMemType { get; set; }
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
        public HashSet<OrderBoxModel> ListOrderBox { get; set; }
    }

    public class OrderBoxModel
    {
        public Guid OrderId { get; set; }
        public Guid BoxId { get; set; }
        public bool IsInBox { get; set; } = false;
    }

    public class PackageDetailResponse
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
        public int TotalQuantity { get; set; }
        public bool IsTaken { get; set; }
        public bool IsInBox { get; set; } = false;
        public List<PackageDetailResponse> PackageShipperDetails { get; set; }
        public HashSet<OrderBoxModel> ListOrderBox { get; set; }
    }

    public class OrderStationDetail
    {
        public Guid BoxId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; }
        public List<PackageOrderDetailModel> PackageOrderDetails { get; set; }
    }

    public class StationOrderModel
    {
        public Guid OrderId { get; set; }

        public List<PackageDetailResponse> StationOrderDetails { get; set; }
    }
}

