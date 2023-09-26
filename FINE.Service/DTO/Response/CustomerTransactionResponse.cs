using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class CustomerTransactionResponse
    {
        public double? Amount { get; set; }

        public string? Notes { get; set; }

        public bool? IsIncrease { get; set; }

        public int? Type { get; set; }

        public int? Status { get; set; }

        public DateTime CreatedAt { get; set; }

    }
}
