﻿namespace FINE.Service.DTO.Request.Store
{
    public class CreateStoreRequest
    {
        public int DestinationId { get; set; }
        public string StoreName { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string? ContactPerson { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}