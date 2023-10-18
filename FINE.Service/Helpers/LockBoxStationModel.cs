using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.Helpers
{
    public class LockBoxInOrderModel
    {
        public List<Guid> ListBoxId { get; set; }
    }

    public class LockBoxinStationModel
    {
        public string StationName { get; set; }
        public Guid StationId { get; set; }
        public int NumberBoxLockPending { get; set; }
    }

}
