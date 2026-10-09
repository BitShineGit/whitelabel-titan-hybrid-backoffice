using System;
using System.Collections.Generic;

namespace Whitelabel_backoffice.Database;

public partial class IntegratedApi
{
    public int Id { get; set; }

    public string ClientId { get; set; } = null!;

    public string ClientSecret { get; set; } = null!;

    public string Endpoint { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string ApiCode { get; set; } = null!;
}
