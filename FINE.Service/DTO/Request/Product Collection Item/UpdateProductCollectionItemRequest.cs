using FINE.Service.Commons;

namespace FINE.Service.DTO.Request.Product_Collection_Item
{
    public class UpdateProductCollectionItemRequest
    {
        public int ProductCollectionId { get; set; }

        public int ProductId { get; set; }

        public bool Active { get; set; }

    }
}
