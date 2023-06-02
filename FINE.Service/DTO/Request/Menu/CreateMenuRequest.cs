using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Menu
{
    public class CreateMenuRequest
    {
        public int TimeSlotId { get; set; }
        [StringLength(20, MinimumLength = 5, ErrorMessage = "A name must be between five and twenty characters.")]
        public string MenuName { get; set; } = null!;
        public string? ImgUrl { get; set; }
        public bool IsActive { get; set; }

    }
}
