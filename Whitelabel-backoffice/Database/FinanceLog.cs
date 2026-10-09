using System;
using System.Collections.Generic;

namespace Whitelabel_backoffice.Database;

public partial class FinanceLog
{
    public int Id { get; set; }

    public string AgentCode { get; set; } = null!;

    public string AgentPath { get; set; } = null!;

    public int AgentTableId { get; set; }

    public string TargetCode { get; set; } = null!;

    public string TargetPath { get; set; } = null!;

    public int TargetTableId { get; set; }

    /// <summary>
    ///  0:Player, 1:Agent
    /// </summary>
    public byte TargetAccountType { get; set; }

    /// <summary>
    /// 0:Self Deposit, 1:Manual Deposit, 2:Self Withdraw, 3:Manual Withdraw
    /// </summary>
    public byte FinanceType { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal AgentBeforeBalance { get; set; }

    public decimal AgentAfterBalance { get; set; }

    public decimal TargetBeforeBalance { get; set; }

    public decimal TargetAfterBalance { get; set; }

    /// <summary>
    /// 0:Pending, 1:Processing, 2:Completed, 3:Rejected, 4:Failed, 5:Cancelled, 6:AutoCancelled
    /// </summary>
    public byte Status { get; set; }

    public string OrderId { get; set; } = null!;
}
