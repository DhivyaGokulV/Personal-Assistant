using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.Health;
using PersonalAssistant.Application.PasswordVault;
using PersonalAssistant.Application.TimeTracker;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Infrastructure.Health;
using PersonalAssistant.Infrastructure.PasswordVault;
using PersonalAssistant.Infrastructure.Persistence;
using PersonalAssistant.Infrastructure.TimeTracker;

namespace PersonalAssistant.Tests;

public class RemainingModulesServiceTests
{
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
