using FINE.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class MenuByTimeSlotResponse
    {
        public List<MenuResponse> Menus  {get; set; }
        public List<ReOrderResponse>? ReOrders { get; set; }
    }

    public class MenuResponse
    {
        public Guid Id { get; set; }

        public Guid TimeSlotId { get; set; }

        public string MenuName { get; set; } = null!;

        public string ImgUrl { get; set; } = null!;

        public int Position { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }

        public ICollection<ProductResponse>? Products { get; set; }
    }
    public class ReOrderResponse
    {
        public Guid Id { get; set; }
        public DateTime CheckInDate { get; set; }
        public int ItemQuantity { get; set; }
        public string StationName { get; set; }

        public List<ProductInReOrder> ListProductInReOrder { get; set; }
    }
    public class ProductInReOrder
    {
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
    }
}
