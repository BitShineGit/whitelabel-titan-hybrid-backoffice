using System;
using System.Collections.Generic;

namespace Whitelabel_backoffice.Database;

public partial class BlockIp
{
    public int Id { get; set; }

    public string? IpAddress { get; set; }

    public string? Desc { get; set; }

    public DateTime CreatedAt { get; set; }
}
