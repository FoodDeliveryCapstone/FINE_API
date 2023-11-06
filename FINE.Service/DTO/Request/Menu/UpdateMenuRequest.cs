﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ServiceStack.LicenseUtils;

namespace FINE.Service.DTO.Request.Menu
{
    public class UpdateMenuRequest
    {
        public Guid TimeSlotId { get; set; }
        [StringLength(20, MinimumLength = 3, ErrorMessage = "A name must be between three and twenty characters.")]
        public string MenuName { get; set; } = null!;
        public string? ImgUrl { get; set; }
        public bool IsActive { get; set; }
        public int Position { get; set; }

    }
}
