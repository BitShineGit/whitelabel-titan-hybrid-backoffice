using System;
using System.Collections.Generic;

namespace Whitelabel_backoffice.Database;

public partial class Currency
{
    public int Id { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public string CurrencySymbol { get; set; } = null!;

    public string Description { get; set; } = null!;
}
