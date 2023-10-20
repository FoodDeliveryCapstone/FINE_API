using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Response
{
    public class QROrderBoxResponse
    {
        public string Key { get; set; }
        public List<Guid>? ListBox { get; set; }

    }
}