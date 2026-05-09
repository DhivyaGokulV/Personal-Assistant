namespace PersonalAssistant.Application.Finance.Budgets;

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
    decimal PercentUsed);

public record CreateBudgetRequest(
    string Name,
    Guid CategoryId,
    decimal Amount,
    DateOnly From,
    DateOnly To,
    string? Note);

public record UpdateBudgetRequest(
    string Name,
    Guid CategoryId,
    decimal Amount,
    DateOnly From,
    DateOnly To,
    string? Note);

public record BudgetTransactionRow(
    DateOnly Date,
    string Reason,
    string AccountName,
    string? PaymentTypeName,
    decimal Amount);

public record BudgetReport(
    BudgetDto Budget,
    IReadOnlyList<BudgetTransactionRow> Transactions);
