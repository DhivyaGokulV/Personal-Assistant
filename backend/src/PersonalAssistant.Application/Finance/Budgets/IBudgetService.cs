namespace PersonalAssistant.Application.Finance.Budgets;

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetDto>> ListAsync(CancellationToken ct);
    Task<BudgetDto> GetAsync(Guid id, CancellationToken ct);
    Task<BudgetDto> CreateAsync(CreateBudgetRequest req, CancellationToken ct);
    Task<BudgetDto> UpdateAsync(Guid id, UpdateBudgetRequest req, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<BudgetReport> GetReportAsync(Guid id, CancellationToken ct);
}
