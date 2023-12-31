﻿using FINE.Data.Entity;
using FINE.Service.Attributes;

namespace FINE.Service.DTO.Response
{
    public class ProductResponse
    {
        public Guid Id { get; set; }

        public Guid StoreId { get; set; }

        public Guid CategoryId { get; set; }

        public string ProductCode { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public int ProductType { get; set; }

        public string ImageUrl { get; set; } = null!;

        public bool IsActive { get; set; }

        public bool? IsStackable { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }

        public List<ProductAttributeResponse>? ProductAttributes { get; set; }

    }

    public class ProductWithoutAttributeResponse
    {
        public Guid Id { get; set; }

        public Guid StoreId { get; set; }

        public Guid CategoryId { get; set; }

        public string ProductCode { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public int ProductType { get; set; }

        public string ImageUrl { get; set; } = null!;

        public bool IsActive { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }


    public class ReportProduct
    {
        public string? StoreName { get; set; }
        public Guid ProductAttributeId { get; set; }
        public string? ProductName { get; set; }
    }

    public class ReportProductResponse
    {
        public string? StoreName { get; set; }
        public List<ReportProduct> Products { get; set; }
    }
}