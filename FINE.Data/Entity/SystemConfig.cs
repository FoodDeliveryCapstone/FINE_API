using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class SystemConfig
{
    public int Id { get; set; }

    public int CountDownTime { get; set; }

    public int OrderQuantityMinimum { get; set; }

    public int OrderQuantityMaximum { get; set; }
}
