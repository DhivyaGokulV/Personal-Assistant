namespace PersonalAssistant.Application.Finance.Budgets;

public record BudgetEntryDto(Guid Id, Guid CategoryId, string CategoryName, decimal Amount, decimal Spent, decimal Remaining, decimal PercentUsed);

public record BudgetDto(
    Guid Id,
    string Name,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    DateOnly From,
    DateOnly To,
    string? Note,
    decimal Spent,
    decimal Remaining,
    decimal PercentUsed,
    IReadOnlyList<BudgetEntryDto>? Entries = null);

public record BudgetEntryRequest(Guid CategoryId, decimal Amount);

public record CreateBudgetRequest(
    string Name,
    Guid CategoryId,
    decimal Amount,
    DateOnly From,
    DateOnly To,
    string? Note,
    IReadOnlyList<BudgetEntryRequest>? Entries = null);

public record UpdateBudgetRequest(
    string Name,
    Guid CategoryId,
    decimal Amount,
    DateOnly From,
    DateOnly To,
    string? Note,
    IReadOnlyList<BudgetEntryRequest>? Entries = null);

public record BudgetTransactionRow(
    DateOnly Date,
    string Reason,
    string CategoryName,
    string AccountName,
    string? PaymentTypeName,
    decimal Amount);

public record BudgetReport(
    BudgetDto Budget,
    IReadOnlyList<BudgetTransactionRow> Transactions);
