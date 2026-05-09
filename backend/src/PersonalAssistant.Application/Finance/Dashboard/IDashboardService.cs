namespace PersonalAssistant.Application.Finance.Dashboard;

public interface IDashboardService
{
    Task<DashboardView> GetAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
