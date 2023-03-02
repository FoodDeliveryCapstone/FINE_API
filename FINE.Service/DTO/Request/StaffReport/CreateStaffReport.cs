using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.StaffReport
{
    public class CreateStaffReport
    {
        public int StaffId { get; set; }
        public double? TotalRestaurantCost { get; set; }
        public int? TotalOrder { get; set; }
        public int? TotalProduct { get; set; }
        public double? DayIncome { get; set; }
        public string? Note { get; set; }
    }
}
