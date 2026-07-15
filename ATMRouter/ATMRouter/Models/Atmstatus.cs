using System;
using System.Collections.Generic;

namespace ATMRouter.Models;

public partial class Atmstatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Atm> Atms { get; set; } = new List<Atm>();
}
