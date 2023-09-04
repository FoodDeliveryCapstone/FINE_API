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

    public class FillBoxRequest
    {
        public ProductAttribute Product { get; set; }
        public int Quantity { get; set; }
    }

    public class FillBoxResponse
    {
        public int QuantitySuccess { get; set; }
        public CubeModel RemainingLengthSpace { get; set; }
        public CubeModel RemainingWidthSpace { get; set; }
    }

    public class PutIntoBoxRequest
    {
        public CubeModel RemainingSpaceBox { get; set; }
        public CubeModel VolumeOccupied { get; set; }
        public CubeModel RemainingWidthSpace { get; set; }
        public CubeModel VolumeWidthOccupied { get; set; }
        public CubeModel RemainingLengthSpace { get; set; }
        public CubeModel VolumeLengthOccupied { get; set; }
    }
    public class PutIntoBoxResponse
    {
        public bool Success { get; set; }
        public CubeModel RemainingSpace { get; set; }
        public CubeModel VolumeOccupiedN { get; set; }
        public CubeModel RemainingWidthSpaceN { get; set; }
        public CubeModel VolumeWidthOccupiedN { get; set; }
        public CubeModel RemainingLengthSpaceN { get; set; }
        public CubeModel VolumeLengthOccupiedN { get; set; }
    }
}
