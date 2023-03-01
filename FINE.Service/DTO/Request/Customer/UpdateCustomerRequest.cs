using FINE.Service.Commons;

namespace FINE.Service.DTO.Request.Customer
{
    public class UpdateCustomerRequest
    {
        [String]
        public string Name { get; set; }
        [String]
        public string Phone { get; set; }
        public string ImageUrl { get; set; }
    }
}
