using System;
using System.Collections.Generic;

namespace Whitelabel_backoffice.Database;

public partial class Message
{
    public int Id { get; set; }

    public int TargetTableId { get; set; }

    public string TargetCode { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Content { get; set; } = null!;

    public byte Type { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsHighlight { get; set; }

    public bool IsRead { get; set; }

    public byte Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
