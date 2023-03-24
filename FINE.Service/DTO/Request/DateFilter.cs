using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request
{
    public class DateFilter
    {
        [DataType(DataType.DateTime)]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ToDate { get; set; }
    }
}
