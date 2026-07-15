using System;
using System.Collections.Generic;

namespace ATMRouter.Models;

public partial class TransactionServiceStatus
{
    public int ServiceStatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<AtmtransactionStatus> AtmtransactionStatuses { get; set; } = new List<AtmtransactionStatus>();
}
