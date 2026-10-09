using System;
using System.Collections.Generic;

namespace Whitelabel_backoffice.Database;

public partial class Agent
{
    public int Id { get; set; }

    public string AgentLoginName { get; set; } = null!;

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastLoginAt { get; set; }

    public DateTime FirstDepositAt { get; set; }

    public string LoginIp { get; set; } = null!;

    /// <summary>
    /// 1:Owner, 2:FA, 3:SMA, 4:MA, 5:AG, 6: AG1
    /// </summary>
    public byte AgentLevel { get; set; }

    public string AgentPath { get; set; } = null!;

    public int ParentId { get; set; }

    public string Password { get; set; } = null!;

    /// <summary>
    /// 0:Open, 1:Suspend, 2:Locked
    /// </summary>
    public byte Status { get; set; }

    public string UplineCode { get; set; } = null!;

    public string PromoCode { get; set; } = null!;

    public string AgentCode { get; set; } = null!;

    public string AspNetUserId { get; set; } = null!;

    public string NickName { get; set; } = null!;

    public int CurrencyId { get; set; }

    public string TimeZone { get; set; } = null!;
}
