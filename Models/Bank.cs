using System;
using System.Collections.Generic;

namespace ATMRouter.Models;

public partial class Bank
{
    public int BankId { get; set; }

    public string BankName { get; set; } = null!;

    public string? LogoUrl { get; set; }

    public virtual ICollection<BankBranch> BankBranches { get; set; } = new List<BankBranch>();
}
