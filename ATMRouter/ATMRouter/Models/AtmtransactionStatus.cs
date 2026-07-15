using System;
using System.Collections.Generic;

namespace ATMRouter.Models;

public partial class AtmtransactionStatus
{
    public int Atmid { get; set; }

    public int TransactionId { get; set; }

    public int ServiceStatusId { get; set; }

    public DateTime? LastUpdated { get; set; }

    public string? Notes { get; set; }

    public virtual Atm Atm { get; set; } = null!;

    public virtual TransactionServiceStatus ServiceStatus { get; set; } = null!;

    public virtual TransactionType Transaction { get; set; } = null!;
}
