using System;
using System.Collections.Generic;

namespace ATMRouter.Models;

public partial class IssueReport
{
    public int ReportId { get; set; }

    public int Atmid { get; set; }

    public int CategoryId { get; set; }

    public string NationalId { get; set; } = null!;

    public string? Description { get; set; }

    public string ReportStatus { get; set; } = null!;

    public DateTime SubmittedAt { get; set; }

    public virtual Atm Atm { get; set; } = null!;

    public virtual IssueCategory Category { get; set; } = null!;
}
