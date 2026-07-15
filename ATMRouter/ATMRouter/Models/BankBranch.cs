using System;
using System.Collections.Generic;

namespace ATMRouter.Models;

public partial class BankBranch
{
    public int BranchId { get; set; }

    public int BankId { get; set; }

    public string BranchName { get; set; } = null!;

    public string? BankAddress { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public virtual ICollection<Atm> Atms { get; set; } = new List<Atm>();

    public virtual Bank Bank { get; set; } = null!;
}
