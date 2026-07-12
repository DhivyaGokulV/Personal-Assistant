using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.AssetTracker.Investments;
using PersonalAssistant.Application.AssetTracker.LiabilityAccounts;
using PersonalAssistant.Application.AssetTracker.Possessions;
using PersonalAssistant.Application.AssetTracker.PreciousMetals;
using PersonalAssistant.Application.Finance.Budgets;
using PersonalAssistant.Application.Health;
using PersonalAssistant.Application.PasswordVault;
using PersonalAssistant.Application.TimeTracker;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Domain.Finance;
using PersonalAssistant.Infrastructure.Finance.Budgets;
using PersonalAssistant.Infrastructure.AssetTracker.Investments;
using PersonalAssistant.Infrastructure.AssetTracker.LiabilityAccounts;
using PersonalAssistant.Infrastructure.AssetTracker.Possessions;
using PersonalAssistant.Infrastructure.AssetTracker.PreciousMetals;
using PersonalAssistant.Infrastructure.Health;
using PersonalAssistant.Infrastructure.PasswordVault;
using PersonalAssistant.Infrastructure.Persistence;
using PersonalAssistant.Infrastructure.TimeTracker;

namespace PersonalAssistant.Tests;

public class RemainingModulesServiceTests
{
    [Fact]
    public async Task Investments_UseWeightedAverageCost_AndWriteAuditRows()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new InvestmentService(fixture.Db, new TestUser(owner));
        var investment = await service.CreateAsync(new CreateInvestmentRequest(
            "Index fund", "Long term", InvestmentType.UnitBased, "INR", null, "Retirement",
            new DateOnly(2026, 1, 1)), CancellationToken.None);

        await service.AddEntryAsync(investment.Id, new SaveInvestmentEntryRequest(
            InvestmentTxType.Buy, new DateOnly(2026, 1, 2), null, 100, 10, null), CancellationToken.None);
        await service.AddEntryAsync(investment.Id, new SaveInvestmentEntryRequest(
            InvestmentTxType.Buy, new DateOnly(2026, 1, 3), null, 200, 10, null), CancellationToken.None);
        await service.AddEntryAsync(investment.Id, new SaveInvestmentEntryRequest(
            InvestmentTxType.Sell, new DateOnly(2026, 1, 4), null, 180, 5, null), CancellationToken.None);
        await service.AddPriceAsync(investment.Id, new SaveInvestmentPriceRequest(
            new DateOnly(2026, 1, 5), 180), CancellationToken.None);

        var result = (await service.GetAsync(investment.Id, CancellationToken.None)).Investment;

        Assert.Equal(15, result.Units);
        Assert.Equal(2250, result.RemainingCostBasis);
        Assert.Equal(2700, result.CurrentValue);
        Assert.Equal(20, result.ProfitLossPercent);
        Assert.True(await fixture.Db.InvestmentAuditEntries.CountAsync() >= 5);
    }

    [Fact]
    public async Task Investments_RejectBackdatedEntriesThatMakeLedgerNegative()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new InvestmentService(fixture.Db, new TestUser(owner));
        var investment = await service.CreateAsync(new CreateInvestmentRequest(
            "Shares", null, InvestmentType.UnitBased, "INR", null, null,
            new DateOnly(2026, 1, 1)), CancellationToken.None);
        await service.AddEntryAsync(investment.Id, new SaveInvestmentEntryRequest(
            InvestmentTxType.Buy, new DateOnly(2026, 1, 10), null, 100, 10, null), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddEntryAsync(
            investment.Id,
            new SaveInvestmentEntryRequest(InvestmentTxType.Sell, new DateOnly(2026, 1, 5), null, 100, 1, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task AmountBasedInvestments_TrackCreditsAndDebits()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new InvestmentService(fixture.Db, new TestUser(owner));
        var investment = await service.CreateAsync(new CreateInvestmentRequest(
            "Fixed deposit", null, InvestmentType.AmountBased, "INR", null, null,
            new DateOnly(2026, 1, 1)), CancellationToken.None);
        await service.AddEntryAsync(investment.Id, new SaveInvestmentEntryRequest(
            InvestmentTxType.Credit, new DateOnly(2026, 1, 2), null, null, null, 10000), CancellationToken.None);
        await service.AddEntryAsync(investment.Id, new SaveInvestmentEntryRequest(
            InvestmentTxType.Debit, new DateOnly(2026, 1, 3), null, null, null, 2500), CancellationToken.None);

        var result = (await service.GetAsync(investment.Id, CancellationToken.None)).Investment;

        Assert.Equal(7500, result.AmountInvested);
        Assert.Equal(7500, result.CurrentValue);
        Assert.Null(result.ProfitLossPercent);
    }

    [Fact]
    public async Task PreciousMetals_SeedDefaults_AndRejectOversell()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new PreciousMetalService(fixture.Db, new TestUser(owner));

        var list = await service.ListAsync(new PreciousMetalQuery(null), CancellationToken.None);
        Assert.Contains(list.Items, x => x.Name == "Gold 24K");
        Assert.Contains(list.Items, x => x.Name == "Silver");

        var gold = list.Items.Single(x => x.Name == "Gold 24K");
        await service.AddEntryAsync(gold.Id, new SavePreciousMetalEntryRequest(
            PreciousMetalTxType.Buy, DateOnly.FromDateTime(DateTime.UtcNow), null, 10, 6000), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddEntryAsync(gold.Id,
            new SavePreciousMetalEntryRequest(PreciousMetalTxType.Sell, DateOnly.FromDateTime(DateTime.UtcNow), null, 11, 6100),
            CancellationToken.None));
    }

    [Fact]
    public async Task Jewellery_CanBeSoldAndRevertedToInPossession()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new JewelleryService(fixture.Db, new TestUser(owner));
        var jewel = await service.CreateAsync(new SaveJewelleryRequest(
            "Ring", "Wedding", new DateOnly(2026, 1, 1), 50000, 8, null), CancellationToken.None);

        var sold = await service.SellAsync(jewel.Id, new SellPossessionRequest(
            "Upgrade", new DateOnly(2026, 1, 2), 55000), CancellationToken.None);
        Assert.Equal(AssetStatus.Sold, sold.Status);

        var reverted = await service.UpdateAsync(jewel.Id, new SaveJewelleryRequest(
            "Ring", "Wedding", new DateOnly(2026, 1, 1), 50000, 8, AssetStatus.InPossession), CancellationToken.None);

        Assert.Equal(AssetStatus.InPossession, reverted.Status);
        Assert.Null(reverted.SellingDate);
        Assert.True(await fixture.Db.JewelleryAuditEntries.CountAsync() >= 3);
    }

    [Fact]
    public async Task PersonalAssets_CanBeSoldAndRevertedToInPossession()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new PersonalAssetService(fixture.Db, new TestUser(owner));
        var asset = await service.CreateAsync(new SavePersonalAssetRequest(
            "Bike", "Commuter", new DateOnly(2026, 1, 1), 90000, null), CancellationToken.None);

        await service.SellAsync(asset.Id, new SellPossessionRequest(
            "Sold to friend", new DateOnly(2026, 1, 3), 70000), CancellationToken.None);
        var reverted = await service.UpdateAsync(asset.Id, new SavePersonalAssetRequest(
            "Bike", "Commuter", new DateOnly(2026, 1, 1), 90000, AssetStatus.InPossession), CancellationToken.None);

        Assert.Equal(AssetStatus.InPossession, reverted.Status);
        Assert.Null(reverted.SellingPrice);
    }

    [Fact]
    public async Task LiabilityAccounts_UseCreditAsIncrease_AndDebitAsPayment()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new LiabilityAccountService(fixture.Db, new TestUser(owner));
        var loan = await service.CreateAsync(LiabilityAccountCategory.Loan, new SaveLiabilityAccountRequest(
            "Education loan", "College", new DateOnly(2026, 1, 1)), CancellationToken.None);

        await service.AddEntryAsync(LiabilityAccountCategory.Loan, loan.Id, new SaveLiabilityAccountEntryRequest(
            LiabilityAccountTxType.Credit, new DateOnly(2026, 1, 2), "Principal", 100000), CancellationToken.None);
        await service.AddEntryAsync(LiabilityAccountCategory.Loan, loan.Id, new SaveLiabilityAccountEntryRequest(
            LiabilityAccountTxType.Debit, new DateOnly(2026, 1, 3), "Installment", 25000), CancellationToken.None);

        var result = await service.ListAsync(LiabilityAccountCategory.Loan, new LiabilityAccountQuery(null, null), CancellationToken.None);

        Assert.Equal(75000, result.Items.Single(x => x.Id == loan.Id).StandingAmount);
        Assert.True(await fixture.Db.LiabilityAccountAuditEntries.CountAsync() >= 3);
    }

    [Fact]
    public async Task LiabilityAccounts_RejectDebitThatMakesStandingAmountNegative()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new LiabilityAccountService(fixture.Db, new TestUser(owner));
        var debt = await service.CreateAsync(LiabilityAccountCategory.Debt, new SaveLiabilityAccountRequest(
            "IOU", null, new DateOnly(2026, 1, 1)), CancellationToken.None);
        await service.AddEntryAsync(LiabilityAccountCategory.Debt, debt.Id, new SaveLiabilityAccountEntryRequest(
            LiabilityAccountTxType.Credit, new DateOnly(2026, 1, 10), null, 1000), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddEntryAsync(
            LiabilityAccountCategory.Debt,
            debt.Id,
            new SaveLiabilityAccountEntryRequest(LiabilityAccountTxType.Debit, new DateOnly(2026, 1, 5), null, 1),
            CancellationToken.None));
    }

    [Fact]
    public async Task LiabilityAccounts_InactiveStatusRequiresZeroStandingAmount()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new LiabilityAccountService(fixture.Db, new TestUser(owner));
        var card = await service.CreateAsync(LiabilityAccountCategory.CreditCard, new SaveLiabilityAccountRequest(
            "Travel card", null, new DateOnly(2026, 1, 1)), CancellationToken.None);
        await service.AddEntryAsync(LiabilityAccountCategory.CreditCard, card.Id, new SaveLiabilityAccountEntryRequest(
            LiabilityAccountTxType.Credit, new DateOnly(2026, 1, 2), null, 5000), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChangeStatusAsync(
            LiabilityAccountCategory.CreditCard,
            card.Id,
            new ChangeLiabilityAccountStatusRequest(LiabilityAccountStatus.Inactive, new DateOnly(2026, 1, 3)),
            CancellationToken.None));

        await service.AddEntryAsync(LiabilityAccountCategory.CreditCard, card.Id, new SaveLiabilityAccountEntryRequest(
            LiabilityAccountTxType.Debit, new DateOnly(2026, 1, 4), null, 5000), CancellationToken.None);
        var status = await service.ChangeStatusAsync(LiabilityAccountCategory.CreditCard, card.Id,
            new ChangeLiabilityAccountStatusRequest(LiabilityAccountStatus.Inactive, new DateOnly(2026, 1, 5)), CancellationToken.None);

        Assert.Equal(LiabilityAccountStatus.Inactive, status.Status);
    }

    [Fact]
    public async Task TimeEntries_AreOwnerScoped_AndReportSummarizesActivity()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new TimeTrackerService(fixture.Db, new TestUser(owner));

        await service.CreateAsync(new TimeEntryRequest(
            new DateTime(2026, 5, 13, 9, 0, 0),
            new DateTime(2026, 5, 13, 10, 30, 0),
            "Deep work", null, "coding"), CancellationToken.None);

        fixture.Db.TimeEntries.Add(new()
        {
            OwnerUserId = Guid.NewGuid(),
            StartTime = new DateTime(2026, 5, 13, 9, 0, 0),
            EndTime = new DateTime(2026, 5, 13, 10, 0, 0),
            Activity = "Other user"
        });
        await fixture.Db.SaveChangesAsync();

        var page = await service.ListAsync(new TimeEntryFilters(null, null, null, null), 1, 25, CancellationToken.None);
        var report = await service.GetReportAsync(new TimeEntryFilters(
            new DateTime(2026, 5, 13, 0, 0, 0),
            new DateTime(2026, 5, 13, 23, 59, 0),
            null, null), CancellationToken.None);

        Assert.Single(page.Items);
        Assert.Equal("Deep work", report.ActivitySummary.Single().Key);
        Assert.Equal(90, report.ActivitySummary.Single().NumberOfMinutes);
    }

    [Fact]
    public async Task TimeEntries_RejectEndBeforeStart()
    {
        await using var fixture = await DbFixture.CreateAsync();
        var service = new TimeTrackerService(fixture.Db, new TestUser(Guid.NewGuid()));

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(new TimeEntryRequest(
            new DateTime(2026, 5, 13, 10, 0, 0),
            new DateTime(2026, 5, 13, 9, 0, 0),
            "Invalid", null, null), CancellationToken.None));
    }

    [Fact]
    public async Task Measurements_RequireAtLeastOneValue()
    {
        await using var fixture = await DbFixture.CreateAsync();
        var service = new HealthService(fixture.Db, new TestUser(Guid.NewGuid()));

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateMeasurementAsync(new MeasurementEntryRequest(
            DateOnly.FromDateTime(DateTime.UtcNow),
            null, null, null, null, null, null, null, null, null, null, null, null, null), CancellationToken.None));
    }

    [Fact]
    public async Task Workouts_ReportProgressiveOverloadVolume()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new HealthService(fixture.Db, new TestUser(owner));

        await service.CreateWorkoutAsync(new WorkoutEntryRequest(
            new DateOnly(2026, 5, 13), WorkoutType.WeightBased, "Bench press", "Chest", "push",
            null, null, null, null, null,
            new[] { new WorkoutSetRequest(10, 40, null), new WorkoutSetRequest(8, 45, null) }), CancellationToken.None);

        var report = await service.GetWorkoutReportAsync(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31), "Bench", CancellationToken.None);

        Assert.Single(report.Rows);
        Assert.Equal(760, report.Rows.Single().TotalVolume);
        Assert.Equal(45, report.Rows.Single().MaxWeight);
        Assert.Equal(18, report.Rows.Single().TotalReps);
    }

    [Fact]
    public async Task PasswordVault_StoresOnlyCiphertext()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new PasswordVaultService(fixture.Db, new TestUser(owner));

        await service.InitializeAsync(new InitializeVaultRequest("salt", "verifier-cipher", "verifier-iv", 100000), CancellationToken.None);
        var group = await service.CreateGroupAsync(new PasswordGroupRequest("Bank", null), CancellationToken.None);
        await service.CreateEntryAsync(new PasswordEntryRequest(
            group.Id, "Net banking", true, false,
            new EncryptedFieldDto("encrypted-user", "iv-user"),
            null,
            new EncryptedFieldDto("encrypted-password", "iv-pass"),
            new DateOnly(2026, 5, 13), null), CancellationToken.None);

        var row = await fixture.Db.PasswordEntries.SingleAsync();
        Assert.DoesNotContain("plain", row.PasswordCipherText ?? "");
        Assert.Equal("encrypted-password", row.PasswordCipherText);
        Assert.Null(row.EmailCipherText);
    }

    [Fact]
    public async Task PasswordVault_ResetMasterPassword_UpdatesOnlyWrappedMasterMetadata()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var service = new PasswordVaultService(fixture.Db, new TestUser(owner));

        await service.InitializeAsync(new InitializeVaultRequest(
            "old-salt", "verifier-cipher", "verifier-iv", 100000,
            "old-wrapped-key", "old-wrapped-iv",
            "pin-salt", "pin-verifier-cipher", "pin-verifier-iv",
            "pin-wrapped-key", "pin-wrapped-iv", 100000), CancellationToken.None);

        var status = await service.ResetMasterPasswordAsync(new ResetMasterPasswordRequest(
            "new-salt", "new-verifier-cipher", "new-verifier-iv", 150000,
            "new-wrapped-key", "new-wrapped-iv"), CancellationToken.None);

        Assert.Equal("new-salt", status.Salt);
        Assert.Equal("new-verifier-cipher", status.VerifierCipherText);
        Assert.Equal("new-wrapped-key", status.MasterWrappedKeyCipherText);
        Assert.Equal("pin-wrapped-key", status.RecoveryWrappedKeyCipherText);

        var row = await fixture.Db.PasswordVaultSettings.SingleAsync();
        Assert.Equal("new-wrapped-key", row.MasterWrappedKeyCipherText);
        Assert.DoesNotContain("password", row.MasterWrappedKeyCipherText ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Budgets_SupportMultipleCategoryAllocations()
    {
        var owner = Guid.NewGuid();
        await using var fixture = await DbFixture.CreateAsync();
        var groceries = new Category { OwnerUserId = owner, Name = "Groceries", Type = CategoryType.Need };
        var dining = new Category { OwnerUserId = owner, Name = "Dining", Type = CategoryType.Want };
        var account = new Account { OwnerUserId = owner, Name = "Checking", OpeningBalance = 0, OpeningDate = new DateOnly(2026, 6, 1), Status = AccountStatus.Active };
        fixture.Db.AddRange(groceries, dining, account);
        await fixture.Db.SaveChangesAsync();

        var service = new BudgetService(fixture.Db, new TestUser(owner));
        var budget = await service.CreateAsync(new CreateBudgetRequest(
            "Food",
            groceries.Id,
            0,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30),
            null,
            new[]
            {
                new BudgetEntryRequest(groceries.Id, 6000),
                new BudgetEntryRequest(dining.Id, 3000)
            }), CancellationToken.None);

        fixture.Db.Transactions.AddRange(
            new Transaction { OwnerUserId = owner, Date = new DateOnly(2026, 6, 3), Type = TransactionType.Debit, AccountId = account.Id, CategoryId = groceries.Id, Amount = 1250, Reason = "Vegetables" },
            new Transaction { OwnerUserId = owner, Date = new DateOnly(2026, 6, 4), Type = TransactionType.Debit, AccountId = account.Id, CategoryId = dining.Id, Amount = 900, Reason = "Dinner" });
        await fixture.Db.SaveChangesAsync();

        var report = await service.GetReportAsync(budget.Id, CancellationToken.None);

        Assert.Equal(9000, report.Budget.Amount);
        Assert.Equal(2150, report.Budget.Spent);
        Assert.Equal("2 categories", report.Budget.CategoryName);
        Assert.Equal(2, report.Budget.Entries!.Count);
        Assert.Contains(report.Budget.Entries, e => e.CategoryName == "Groceries" && e.Spent == 1250 && e.Remaining == 4750);
        Assert.Contains(report.Budget.Entries, e => e.CategoryName == "Dining" && e.Spent == 900 && e.Remaining == 2100);
        Assert.Contains(report.Transactions, t => t.CategoryName == "Groceries");
    }

    private sealed record TestUser(Guid Id) : IUserContext
    {
        public Guid? UserId => Id;
        public string? Email => "test@example.com";
        public bool IsAuthenticated => true;
    }

    private sealed class DbFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext Db { get; }

        private DbFixture(SqliteConnection connection, AppDbContext db)
        {
            _connection = connection;
            Db = db;
        }

        public static async Task<DbFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();
            return new DbFixture(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
