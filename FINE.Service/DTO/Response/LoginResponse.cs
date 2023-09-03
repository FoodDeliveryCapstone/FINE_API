using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class LoginResponse
    {
        public string Access_token { get; set; }

        public bool IsFirstLogin { get; set; }
        public CustomerResponse Customer { get; set; }
    }
}
