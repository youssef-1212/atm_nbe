using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ATMRouter.Models;

public partial class AtmrouterContext : DbContext
{
    public AtmrouterContext()
    {
    }

    public AtmrouterContext(DbContextOptions<AtmrouterContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Atm> Atms { get; set; }

    public virtual DbSet<Atmstatus> Atmstatuses { get; set; }

    public virtual DbSet<AtmtransactionStatus> AtmtransactionStatuses { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Bank> Banks { get; set; }

    public virtual DbSet<BankBranch> BankBranches { get; set; }

    public virtual DbSet<CashInventory> CashInventories { get; set; }

    public virtual DbSet<IssueCategory> IssueCategories { get; set; }

    public virtual DbSet<IssueReport> IssueReports { get; set; }

    public virtual DbSet<TransactionServiceStatus> TransactionServiceStatuses { get; set; }

    public virtual DbSet<TransactionType> TransactionTypes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Connection string is configured in Program.cs
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Latin1_General_CI_AS");

        modelBuilder.Entity<Atm>(entity =>
        {
            entity.HasKey(e => e.Atmid).HasName("PK__ATM__C4B5AFA978DD91E6");

            entity.ToTable("ATM");

            entity.HasIndex(e => e.Atmcode, "UQ__ATM__328EECF1260F5234").IsUnique();

            entity.Property(e => e.Atmid).HasColumnName("ATMID");
            entity.Property(e => e.Atmcode)
                .HasMaxLength(100)
                .HasColumnName("ATMCode");
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.IsOperational).HasDefaultValue(true);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Latitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");

            entity.HasOne(d => d.Branch).WithMany(p => p.Atms)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ATM_Branch");

            entity.HasOne(d => d.Status).WithMany(p => p.Atms)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ATM_Status");

            entity.HasMany(d => d.Transactions).WithMany(p => p.Atms)
                .UsingEntity<Dictionary<string, object>>(
                    "Atmtransaction",
                    r => r.HasOne<TransactionType>().WithMany()
                        .HasForeignKey("TransactionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__ATMTransa__Trans__0B91BA14"),
                    l => l.HasOne<Atm>().WithMany()
                        .HasForeignKey("Atmid")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__ATMTransa__ATMID__0A9D95DB"),
                    j =>
                    {
                        j.HasKey("Atmid", "TransactionId").HasName("PK__ATMTrans__61E19C0D2D8594A4");
                        j.ToTable("ATMTransaction");
                        j.IndexerProperty<int>("Atmid").HasColumnName("ATMID");
                        j.IndexerProperty<int>("TransactionId").HasColumnName("TransactionID");
                    });
        });

        modelBuilder.Entity<Atmstatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__ATMStatu__C8EE20435542FCD3");

            entity.ToTable("ATMStatus");

            entity.HasIndex(e => e.StatusName, "UQ__ATMStatu__05E7698A5FBC62D2").IsUnique();

            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.StatusName).HasMaxLength(100);
        });

        modelBuilder.Entity<AtmtransactionStatus>(entity =>
        {
            entity.HasKey(e => new { e.Atmid, e.TransactionId }).HasName("PK__ATMTrans__61E19C0DCD7940EB");

            entity.ToTable("ATMTransactionStatus");

            entity.Property(e => e.Atmid).HasColumnName("ATMID");
            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.ServiceStatusId).HasColumnName("ServiceStatusID");

            entity.HasOne(d => d.Atm).WithMany(p => p.AtmtransactionStatuses)
                .HasForeignKey(d => d.Atmid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ATMTransa__ATMID__1CBC4616");

            entity.HasOne(d => d.ServiceStatus).WithMany(p => p.AtmtransactionStatuses)
                .HasForeignKey(d => d.ServiceStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ATMTransa__Servi__1EA48E88");

            entity.HasOne(d => d.Transaction).WithMany(p => p.AtmtransactionStatuses)
                .HasForeignKey(d => d.TransactionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ATMTransa__Trans__1DB06A4F");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__AuditLog__5E5499A82708862C");

            entity.ToTable("AuditLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EntityId).HasColumnName("EntityID");
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.EventType).HasMaxLength(50);
        });

        modelBuilder.Entity<Bank>(entity =>
        {
            entity.HasKey(e => e.BankId).HasName("PK__Bank__AA08CB3304AF5260");

            entity.ToTable("Bank");

            entity.HasIndex(e => e.BankName, "UQ__Bank__DA9ADFAAFB780136").IsUnique();

            entity.Property(e => e.BankId).HasColumnName("BankID");
            entity.Property(e => e.BankName).HasMaxLength(50);
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(256)
                .HasColumnName("LogoURL");
        });

        modelBuilder.Entity<BankBranch>(entity =>
        {
            entity.HasKey(e => e.BranchId).HasName("PK__BankBran__A1682FA5AA19285A");

            entity.ToTable("BankBranch");

            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.BankAddress).HasMaxLength(256);
            entity.Property(e => e.BankId).HasColumnName("BankID");
            entity.Property(e => e.BranchName).HasMaxLength(100);
            entity.Property(e => e.Latitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9, 6)");

            entity.HasOne(d => d.Bank).WithMany(p => p.BankBranches)
                .HasForeignKey(d => d.BankId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Branch_Bank");
        });

        modelBuilder.Entity<CashInventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK__CashInve__F5FDE6D384B72F92");

            entity.ToTable("CashInventory");

            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.Atmid).HasColumnName("ATMID");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValue("EGP");
            entity.Property(e => e.Denomination).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Atm).WithMany(p => p.CashInventories)
                .HasForeignKey(d => d.Atmid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CashInven__ATMID__10566F31");
        });

        modelBuilder.Entity<IssueCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__IssueCat__19093A2B21A20094");

            entity.ToTable("IssueCategory");

            entity.HasIndex(e => e.CategoryName, "UQ__IssueCat__8517B2E052BDA17E").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<IssueReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__IssueRep__D5BD48E5F530290C");

            entity.ToTable("IssueReport");

            entity.Property(e => e.ReportId).HasColumnName("ReportID");
            entity.Property(e => e.Atmid).HasColumnName("ATMID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.NationalId)
                .HasMaxLength(14)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("NationalID");
            entity.Property(e => e.ReportStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Atm).WithMany(p => p.IssueReports)
                .HasForeignKey(d => d.Atmid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__IssueRepo__ATMID__30C33EC3");

            entity.HasOne(d => d.Category).WithMany(p => p.IssueReports)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__IssueRepo__Categ__31B762FC");
        });

        modelBuilder.Entity<TransactionServiceStatus>(entity =>
        {
            entity.HasKey(e => e.ServiceStatusId).HasName("PK__Transact__009D5EB9C37D32A6");

            entity.ToTable("TransactionServiceStatus");

            entity.HasIndex(e => e.StatusName, "UQ__Transact__05E7698AA3A4A03D").IsUnique();

            entity.Property(e => e.ServiceStatusId).HasColumnName("ServiceStatusID");
            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<TransactionType>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Transact__55433A4B7718640D");

            entity.ToTable("TransactionType");

            entity.HasIndex(e => e.TransactionName, "UQ__Transact__5ACC27A6EC225C4F").IsUnique();

            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.TransactionName).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
