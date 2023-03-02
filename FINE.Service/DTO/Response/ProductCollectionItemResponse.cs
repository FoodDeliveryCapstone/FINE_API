using FINE.Service.Commons;

namespace FINE.Service.DTO.Response
{
    public class ProductCollectionItemResponse
    {
        public int Id { get; set; }

        public int ProductCollectionId { get; set; }

        public int ProductId { get; set; }

        public bool Active { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }
}
