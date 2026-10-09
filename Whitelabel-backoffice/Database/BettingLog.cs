using System;
using System.Collections.Generic;

namespace Whitelabel_backoffice.Database;

public partial class BettingLog
{
    public int Id { get; set; }

    public int UserTableId { get; set; }

    public string UserCode { get; set; } = null!;

    public int AgentTableId { get; set; }

    public string AgentCode { get; set; } = null!;

    public string AgentPath { get; set; } = null!;

    public int ProviderId { get; set; }

    public string ProviderName { get; set; } = null!;

    public string ProviderCode { get; set; } = null!;

    public string GameName { get; set; } = null!;

    public string GameCode { get; set; } = null!;

    public string RoundId { get; set; } = null!;

    public decimal BetAmount { get; set; }

    public decimal WinAmount { get; set; }

    public decimal BeforeBalance { get; set; }

    public decimal AfterBalance { get; set; }

    public byte Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public long HistoryId { get; set; }
}
