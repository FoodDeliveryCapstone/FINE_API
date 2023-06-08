using FINE.Service.Attributes;

namespace FINE.Service.DTO.Request.Product_Collection_Item
{
    public class CreateProductCollectionItemRequest
    {

        public int ProductCollectionId { get; set; }

        public int ProductId { get; set; }

        public bool Active { get; set; }

    }
}
