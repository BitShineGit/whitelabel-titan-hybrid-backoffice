using System;
using System.Collections.Generic;

namespace MyStake.Models;

public partial class GameListMedel
{
    public int Id { get; set; }

    public int ProviderId { get; set; }

    public string GameCode { get; set; } = null!;

    public string GameName { get; set; } = null!;

    public byte GameType { get; set; }

    public string Thumbnail { get; set; } = null!;

    public byte Status { get; set; }

    public bool IsHot { get; set; }

    public bool IsNew { get; set; }

    public int SortNumber { get; set; }
    public string VendorCode { get; set; }
}
