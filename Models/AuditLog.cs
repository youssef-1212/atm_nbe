using System;
using System.Collections.Generic;

namespace ATMRouter.Models;

public partial class AuditLog
{
    public int LogId { get; set; }

    public string EventType { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public int? EntityId { get; set; }

    public string Description { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
