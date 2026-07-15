using System;
using System.Collections.Generic;

namespace ATMRouter.Models;

public partial class TransactionType
{
    public int TransactionId { get; set; }

    public string TransactionName { get; set; } = null!;

    public virtual ICollection<AtmtransactionStatus> AtmtransactionStatuses { get; set; } = new List<AtmtransactionStatus>();

    public virtual ICollection<Atm> Atms { get; set; } = new List<Atm>();
}
