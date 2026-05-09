using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Domain.AssetTracker;
using PersonalAssistant.Domain.Finance;
using PersonalAssistant.Domain.Identity;
using PersonalAssistant.Domain.Tasks.Daily;
using PersonalAssistant.Domain.Tasks.Periodic;
using PersonalAssistant.Domain.Tasks.Todo;

namespace PersonalAssistant.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DailyTaskGroup> DailyTaskGroups => Set<DailyTaskGroup>();
    public DbSet<DailyTask> DailyTasks => Set<DailyTask>();
    public DbSet<DailyTaskCompletion> DailyTaskCompletions => Set<DailyTaskCompletion>();

    public DbSet<PeriodicTaskGroup> PeriodicTaskGroups => Set<PeriodicTaskGroup>();
    public DbSet<PeriodicTask> PeriodicTasks => Set<PeriodicTask>();
    public DbSet<PeriodicTaskHistory> PeriodicTaskHistory => Set<PeriodicTaskHistory>();

    public DbSet<TodoItem> Todos => Set<TodoItem>();

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<PaymentType> PaymentTypes => Set<PaymentType>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionTag> TransactionTags => Set<TransactionTag>();
    public DbSet<Budget> Budgets => Set<Budget>();

    public DbSet<AssetTag> AssetTags => Set<AssetTag>();
    public DbSet<AssetGroup> AssetGroups => Set<AssetGroup>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetPriceHistory> AssetPriceHistory => Set<AssetPriceHistory>();
    public DbSet<InvestmentGroup> InvestmentGroups => Set<InvestmentGroup>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<InvestmentPriceHistory> InvestmentPriceHistory => Set<InvestmentPriceHistory>();
    public DbSet<InvestmentTransaction> InvestmentTransactions => Set<InvestmentTransaction>();
    public DbSet<Liability> Liabilities => Set<Liability>();
    public DbSet<LiabilityHistory> LiabilityHistory => Set<LiabilityHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
