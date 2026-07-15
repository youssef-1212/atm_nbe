using System;
using System.Collections.Generic;

namespace ATMRouter.Models;

public partial class Atm
{
    public int Atmid { get; set; }

    public string Atmcode { get; set; } = null!;

    public int BranchId { get; set; }

    public int StatusId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public bool? IsOperational { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual ICollection<AtmtransactionStatus> AtmtransactionStatuses { get; set; } = new List<AtmtransactionStatus>();

    public virtual BankBranch Branch { get; set; } = null!;

    public virtual ICollection<CashInventory> CashInventories { get; set; } = new List<CashInventory>();

    public virtual ICollection<IssueReport> IssueReports { get; set; } = new List<IssueReport>();

    public virtual Atmstatus Status { get; set; } = null!;

    public virtual ICollection<TransactionType> Transactions { get; set; } = new List<TransactionType>();
}
