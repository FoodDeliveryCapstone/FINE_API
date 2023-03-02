using FINE.Service.Commons;

namespace FINE.Service.DTO.Request.Product_Collection_Time_Slot
{
    public class CreateProductCollectionTimeSlotRequest
    {
        public int ProductCollectionId { get; set; }

        public int TimeSlotId { get; set; }

        public int Position { get; set; }

        public bool IsShownAtHome { get; set; }

        public bool Active { get; set; }

    }
}
