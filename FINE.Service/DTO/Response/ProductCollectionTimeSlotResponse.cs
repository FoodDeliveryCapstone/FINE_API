namespace FINE.Service.DTO.Response
{
    public class ProductCollectionTimeSlotResponse
    {
        public int Id { get; set; }

        public int ProductCollectionId { get; set; }

        public int TimeSlotId { get; set; }

        public int Position { get; set; }

        public bool IsShownAtHome { get; set; }

        public bool Active { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

    }
}
