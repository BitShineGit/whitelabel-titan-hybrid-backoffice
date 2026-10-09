using System;
using System.Collections.Generic;

namespace Whitelabel_backoffice.Database;

public partial class Provider
{
    public int Id { get; set; }

    public string ProviderCode { get; set; } = null!;

    public string ProviderName { get; set; } = null!;

    public string Memo { get; set; } = null!;

    public string Logo { get; set; } = null!;

    public byte ProviderType { get; set; }

    public byte Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int TotalGameCount { get; set; }
}
