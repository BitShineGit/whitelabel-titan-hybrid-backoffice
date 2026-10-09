using System;
using System.Collections.Generic;

namespace Whitelabel_backoffice.Database;

public partial class User
{
    public int Id { get; set; }

    public string UserCode { get; set; } = null!;

    public string UserNickName { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public decimal TotalBalance { get; set; }

    public decimal CurrentBalance { get; set; }

    public string Password { get; set; } = null!;

    public string CurrencyCode { get; set; } = null!;

    public byte Status { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string UplineCode { get; set; } = null!;

    public DateTime LastLoginAt { get; set; }

    public DateTime FirstDepositAt { get; set; }

    public string LastLoginIp { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int AgentTableId { get; set; }

    public string AgentCode { get; set; } = null!;

    public byte AgentLevel { get; set; }

    public string AgentPath { get; set; } = null!;

    public string PromoCode { get; set; } = null!;

    public int CurrencyId { get; set; }

    public decimal TotalBetAmount { get; set; }

    public decimal TotalWinAmount { get; set; }

    public bool IsFinishedFirstDeposit { get; set; }

    public decimal TotalDepositAmount { get; set; }

    public decimal TotalWithdrawAmount { get; set; }

    public DateTime? FirstWithdrawAt { get; set; }

    public decimal WelcomeBonusAmount { get; set; }

    public DateTime? LastDepositAt { get; set; }

    public DateTime? LastWithdrawAt { get; set; }

    public decimal TotalReceivedBonusAmount { get; set; }

    /// <summary>
    /// 0: Request, 1: Verified, 2: Rejected, 3: NotSubmitted
    /// </summary>
    public byte KycStatus { get; set; }

    public string? Country { get; set; }

    public string? Region { get; set; }

    public string? Birthday { get; set; }

    public string? Gender { get; set; }
}
