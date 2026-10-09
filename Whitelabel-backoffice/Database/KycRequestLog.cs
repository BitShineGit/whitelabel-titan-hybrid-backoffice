using System;
using System.Collections.Generic;

namespace Whitelabel_backoffice.Database;

public partial class KycRequestLog
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime RequestTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 0: Request, 1: Verified, 2: Rejected, 3: NotSubmitted
    /// </summary>
    public byte Status { get; set; }
}
