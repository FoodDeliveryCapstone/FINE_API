﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class SimulateResponse
    {
        public SimulateSingleOrderResponse SingleOrderResult { get; set; }
        public SimulateCoOrderResponse CoOrderOrderResult { get; set; }
    }

    public class SimulateSingleOrderResponse
    {
        public List<OrderSimulateResponse> OrderSuccess { get; set; }
        public List<OrderSimulateResponse> OrderFailed { get; set; }

    }
    public class OrderSimulateResponse
    {
        public string? CustomerName { get; set; }
        public string Message { get; set; }
        public List<GroupedOrderDetail>? OrderDetails { get; set; }
    }

    public class SimulateCoOrderResponse
    {
        public List<OrderSimulateResponse> OrderSuccess { get; set; }
        public List<OrderSimulateResponse> OrderFailed { get; set; }
    }

    public class OrderSuccessOrderDetail
    {
        public Guid? StoreId { get; set; }
        public string? ProductName { get; set; } = null!;
        public int Quantity { get; set; }
    }

    public class GroupedOrderDetail
    {
        public Guid? StoreId { get; set; }
        public string? StoreName { get; set; }
        public List<ProductAndQuantity>? ProductAndQuantity { get; set; }
    }

    public class ProductAndQuantity
    {
        public string? ProductName { get; set; } = null!;
        public int Quantity { get; set; }
    }

    public class SimulateOrderForStaffResponse
    {
        public List<SimulateOrderForStaff> OrderSuccess { get; set; }
        public List<SimulateOrderForStaff> OrderFailed { get; set; }
    }

    public class SimulateOrderForStaff
    {
        public string? Message { get; set; }
        public Guid? StoreId { get; set; }
        public string? StaffName { get; set; }
        public string? ProductName { get; set; } = null!;
        public int? Quantity { get; set; }
    }

    public class SimulateOrderStatusResponse
    {
        public string? OrderCode { get; set; }
        public int ItemQuantity { get; set; }
        public string? CustomerName { get; set; }
    }

    public class SimulateOrderForShipperResponse
    {
        public List<SimulateOrderForShipper> OrderSuccess { get; set; }
        public List<SimulateOrderForShipper> OrderFailed { get; set; }
    }

    public class SimulateOrderForShipper
    {
        public string? Message { get; set; }
        public Guid? StoreId { get; set; }
        public string? StaffName { get; set; }
        public List<ProductAndQuantity>? ProductAndQuantities { get; set; }
    }
}
