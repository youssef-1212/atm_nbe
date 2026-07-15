using System;
using System.Collections.Generic;

namespace ATMRouter.Models;

public partial class CashInventory
{
    public int InventoryId { get; set; }

    public int Atmid { get; set; }

    public string? Currency { get; set; }

    public decimal Denomination { get; set; }

    public int Quantity { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual Atm Atm { get; set; } = null!;
}
