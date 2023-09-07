using FINE.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.Helpers
{
    public class CubeModel
    {
        public double Height { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
    }

    public class CheckFixBoxRequest
    {
        public ProductAttribute Product { get; set; }
        public int Quantity { get; set; }
    }
    public class ProductParingResponse
    {
        public CubeModel ProductOccupied { get; set; }
        public int QuantityCanAdd { get; set; }
    }
    public class FixBoxResponse
    {
        public int QuantitySuccess { get; set; }
        public CubeModel RemainingLengthSpace { get; set; }
        public CubeModel RemainingWidthSpace { get; set; }
    }

    public class SpaceInBoxMode
    {
        public bool Success { get; set; }
        public CubeModel RemainingSpaceBox { get; set; }
        public CubeModel VolumeOccupied { get; set; }
        public CubeModel RemainingWidthSpace { get; set; }
        public CubeModel VolumeWidthOccupied { get; set; }
        public CubeModel RemainingLengthSpace { get; set; }
        public CubeModel VolumeLengthOccupied { get; set; }
    }
}
