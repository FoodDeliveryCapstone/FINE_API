﻿namespace FINE.Service.DTO.Response
{
    public class StoreResponse
    {
        public int Id { get; set; }
        public int DestinationId { get; set; }
        public string? StoreName { get; set; }
        public string? ImageUrl { get; set; }
        public string? ContactPerson { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<ProductResponse>? products { get; set; }
    }
}