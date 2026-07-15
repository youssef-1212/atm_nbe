using System;
using System.Collections.Generic;

namespace ATMRouter.Models;

public partial class IssueCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<IssueReport> IssueReports { get; set; } = new List<IssueReport>();
}
