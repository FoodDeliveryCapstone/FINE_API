﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Request.Order
{
    public class UpdateOrderRequest
    {
        public string DeliveryPhone { get; set; }

        public int? OrderType { get; set; }

        public int RoomId { get; set; }

        public string? Note { get; set; }
    }
    public class UpdateOrderStatusRequest
    {
        public OrderStatusEnum OrderStatus { get; set; }
    }
    
    public class UpdateOrderDetailStatusRequest
    {
        public bool OrderDetailStoreStatus { get; set; }
    }
}
