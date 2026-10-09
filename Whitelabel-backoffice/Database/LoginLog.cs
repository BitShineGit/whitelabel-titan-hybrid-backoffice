using System;
using System.Collections.Generic;

namespace Whitelabel_backoffice.Database;

public partial class LoginLog
{
    public int Id { get; set; }

    public string? UserEmail { get; set; }

    public string? IpAddress { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    public string? RegionName { get; set; }

    public string? Zip { get; set; }

    public DateTime AccessAt { get; set; }
}
