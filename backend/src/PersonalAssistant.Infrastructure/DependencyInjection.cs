using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PersonalAssistant.Application.AssetTracker.Assets;
using PersonalAssistant.Application.AssetTracker.Common;
using PersonalAssistant.Application.AssetTracker.Dashboard;
using PersonalAssistant.Application.AssetTracker.Investments;
using PersonalAssistant.Application.AssetTracker.Liabilities;
using PersonalAssistant.Application.Common.Auth;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Application.Finance.Budgets;
using PersonalAssistant.Application.Finance.Dashboard;
using PersonalAssistant.Application.Finance.Settings;
using PersonalAssistant.Application.Finance.Transactions;
using PersonalAssistant.Application.Tasks.Daily;
using PersonalAssistant.Application.Tasks.Periodic;
using PersonalAssistant.Application.Tasks.Todo;
using PersonalAssistant.Domain.Identity;
using PersonalAssistant.Infrastructure.AssetTracker.Assets;
using PersonalAssistant.Infrastructure.AssetTracker.Common;
using PersonalAssistant.Infrastructure.AssetTracker.Dashboard;
using PersonalAssistant.Infrastructure.AssetTracker.Investments;
using PersonalAssistant.Infrastructure.AssetTracker.Liabilities;
using PersonalAssistant.Infrastructure.Auth;
using PersonalAssistant.Infrastructure.Finance.Budgets;
using PersonalAssistant.Infrastructure.Finance.Dashboard;
using PersonalAssistant.Infrastructure.Finance.Settings;
using PersonalAssistant.Infrastructure.Finance.Transactions;
using PersonalAssistant.Infrastructure.Persistence;
using PersonalAssistant.Infrastructure.Reports;
using PersonalAssistant.Infrastructure.Tasks.Daily;
using PersonalAssistant.Infrastructure.Tasks.Periodic;
using PersonalAssistant.Infrastructure.Tasks.Todo;

namespace PersonalAssistant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuditAndSoftDeleteInterceptor>();

        var provider = configuration["Database:Provider"] ?? "Sqlite";
        services.AddDbContext<AppDbContext>((sp, opt) =>
        {
            opt.AddInterceptors(sp.GetRequiredService<AuditAndSoftDeleteInterceptor>());

            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var cs = configuration["Database:ConnectionStrings:SqlServer"]
                         ?? throw new InvalidOperationException("Missing Database:ConnectionStrings:SqlServer");
                opt.UseSqlServer(cs, x => x.MigrationsAssembly("PersonalAssistant.Migrations.SqlServer"));
            }
            else
            {
                var cs = configuration["Database:ConnectionStrings:Sqlite"]
                         ?? throw new InvalidOperationException("Missing Database:ConnectionStrings:Sqlite");
                opt.UseSqlite(cs, x => x.MigrationsAssembly("PersonalAssistant.Migrations.Sqlite"));
            }
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IDailyTaskService, DailyTaskService>();
        services.AddScoped<IPeriodicTaskService, PeriodicTaskService>();
        services.AddScoped<ITodoService, TodoService>();

        services.AddScoped<IFinanceSettingsService, FinanceSettingsService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IBudgetService, BudgetService>();

        services.AddScoped<IAssetTagService, AssetTagService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IInvestmentService, InvestmentService>();
        services.AddScoped<ILiabilityService, LiabilityService>();
        services.AddScoped<IAssetTrackerDashboardService, AssetTrackerDashboardService>();

        services.AddSingleton<IReportExportService, ReportExportService>();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                  ?? throw new InvalidOperationException("Missing Jwt section");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.SaveToken = true;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();
        return services;
    }
}
