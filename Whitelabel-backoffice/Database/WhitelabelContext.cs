using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Whitelabel_backoffice.Database;

public partial class WhitelabelContext : DbContext
{
    public WhitelabelContext(DbContextOptions<WhitelabelContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Agent> Agents { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<BettingLog> BettingLogs { get; set; }

    public virtual DbSet<BlockIp> BlockIps { get; set; }

    public virtual DbSet<Currency> Currencies { get; set; }

    public virtual DbSet<FinanceLog> FinanceLogs { get; set; }

    public virtual DbSet<Game> Games { get; set; }

    public virtual DbSet<IntegratedApi> IntegratedApis { get; set; }

    public virtual DbSet<KycRequestLog> KycRequestLogs { get; set; }

    public virtual DbSet<LoginLog> LoginLogs { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Provider> Providers { get; set; }

    public virtual DbSet<Setting> Settings { get; set; }

    public virtual DbSet<StatisticsLog> StatisticsLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Korean_Wansung_CS_AS");

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.Property(e => e.AgentCode)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.AgentLevel).HasComment("1:Owner, 2:FA, 3:SMA, 4:MA, 5:AG, 6: AG1");
            entity.Property(e => e.AgentLoginName)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.AgentPath)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.AspNetUserId)
                .HasMaxLength(500)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.Balance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LoginIp)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.NickName)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.PromoCode)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.Status).HasComment("0:Open, 1:Suspend, 2:Locked");
            entity.Property(e => e.TimeZone)
                .HasMaxLength(50)
                .HasDefaultValue("");
            entity.Property(e => e.UplineCode)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
        });

        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedName] IS NOT NULL)");

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedUserName] IS NOT NULL)");

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<BettingLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BettingLogs_1");

            entity.Property(e => e.AfterBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AgentCode)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.AgentPath)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.BeforeBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BetAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GameCode)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.GameName)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.ProviderCode)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.ProviderName)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.RoundId)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.UserCode)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.WinAmount).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<BlockIp>(entity =>
        {
            entity.Property(e => e.Desc).HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(255);
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.Property(e => e.Id).HasComment("");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(50)
                .HasComment("");
            entity.Property(e => e.CurrencySymbol)
                .HasMaxLength(50)
                .HasComment("");
            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .HasDefaultValue("");
        });

        modelBuilder.Entity<FinanceLog>(entity =>
        {
            entity.Property(e => e.AgentAfterBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AgentBeforeBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AgentCode)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.AgentPath)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FinanceType).HasComment("0:Self Deposit, 1:Manual Deposit, 2:Self Withdraw, 3:Manual Withdraw");
            entity.Property(e => e.OrderId)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Status).HasComment("0:Pending, 1:Processing, 2:Completed, 3:Rejected, 4:Failed, 5:Cancelled, 6:AutoCancelled");
            entity.Property(e => e.TargetAccountType).HasComment(" 0:Player, 1:Agent");
            entity.Property(e => e.TargetAfterBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TargetBeforeBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TargetCode)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.TargetPath)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.Property(e => e.GameCode)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.GameName)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.Thumbnail)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
        });

        modelBuilder.Entity<IntegratedApi>(entity =>
        {
            entity.Property(e => e.ApiCode)
                .HasMaxLength(50)
                .HasDefaultValue("");
            entity.Property(e => e.ClientId).HasMaxLength(255);
            entity.Property(e => e.ClientSecret).HasMaxLength(255);
            entity.Property(e => e.Endpoint).HasMaxLength(255);
        });

        modelBuilder.Entity<KycRequestLog>(entity =>
        {
            entity.Property(e => e.Status).HasComment("0: Request, 1: Verified, 2: Rejected, 3: NotSubmitted");
        });

        modelBuilder.Entity<LoginLog>(entity =>
        {
            entity.Property(e => e.City).HasMaxLength(255);
            entity.Property(e => e.Country).HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(255);
            entity.Property(e => e.RegionName).HasMaxLength(255);
            entity.Property(e => e.UserEmail).HasMaxLength(255);
            entity.Property(e => e.Zip).HasMaxLength(255);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.Property(e => e.Content)
                .HasMaxLength(1000)
                .HasDefaultValue("");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasDefaultValue("");
            entity.Property(e => e.Subject)
                .HasMaxLength(500)
                .HasDefaultValue("");
            entity.Property(e => e.TargetCode)
                .HasMaxLength(100)
                .HasDefaultValue("");
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.Property(e => e.Memo).HasMaxLength(100);
            entity.Property(e => e.ProviderCode).HasMaxLength(100);
            entity.Property(e => e.ProviderName).HasMaxLength(100);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => new { e.Category, e.Key }).HasName("PK__Settings__017343ADF2809EE7");

            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.Key)
                .HasMaxLength(100)
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.Value)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
        });

        modelBuilder.Entity<StatisticsLog>(entity =>
        {
            entity.Property(e => e.AgentCode)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.AgentPath)
                .HasMaxLength(500)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.GameCode)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.GameName)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.ProviderCode)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.ProviderName)
                .HasMaxLength(50)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.TotalBetAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalBetCount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalWinAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalWinCount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UserCode)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.AgentCode)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.AgentPath)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.Birthday)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(50)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.CurrentBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Gender)
                .HasMaxLength(50)
                .HasDefaultValue("");
            entity.Property(e => e.KycStatus)
                .HasDefaultValue((byte)3)
                .HasComment("0: Request, 1: Verified, 2: Rejected, 3: NotSubmitted");
            entity.Property(e => e.LastLoginIp)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.PromoCode)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.Region)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.TotalBalance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalBetAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalDepositAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalReceivedBonusAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalWinAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalWithdrawAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UplineCode)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.UserCode)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.UserNickName)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.Property(e => e.WelcomeBonusAmount).HasColumnType("decimal(18, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
