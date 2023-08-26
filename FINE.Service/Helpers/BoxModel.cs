using FINE.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.Helpers
{
    public class BoxModel
    {
        public double Temp { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
    }

    public class FillBoxListResult
    {
        public BoxModel Box { get; set;}
        public List<bool> listCheck { get; set; }
    }
}
