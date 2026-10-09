using System;
using System.Collections.Generic;

namespace Whitelabel_backoffice.Database;

public partial class Setting
{
    public string Category { get; set; } = null!;

    public string Key { get; set; } = null!;

    public string? Value { get; set; }
}
