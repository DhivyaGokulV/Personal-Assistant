namespace PersonalAssistant.Application.AssetTracker.Dashboard;

public interface IAssetTrackerDashboardService
{
    Task<AssetTrackerDashboardView> GetAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
