using FINE.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.Helpers
{
    //public class CubeModel
    //{
    //    public double[] Height { get; set; }
    //    public double[] Width { get; set; }
    //    public double[] Length { get; set; }
    //}

    //public class BoxRemainingSpace
    //{
    //    public CubeModel SpaceOnTopProduct { get; set; }
    //    public CubeModel RemainingSpace { get; set; }
    //}

    public class FillBoxResult
    {
        public double VolumeRemainingSpace { get; set;}

        public bool Success { get; set;}
    }
}
