using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.Finance.Transactions;

public record TagBadge(Guid Id, string Name, string Color);

public record TransactionDto(
    Guid Id,
    DateOnly Date,
    TransactionType Type,
    Guid AccountId,
    string AccountName,
    decimal Amount,
    string Reason,
    string? Note,
    Guid? CategoryId,
    string? CategoryName,
    Guid? PaymentTypeId,
    string? PaymentTypeName,
    Guid? TransferGroupId,
    IReadOnlyList<TagBadge> Tags,
    decimal? AccountStanding);

public record CreateTransactionRequest(
    DateOnly Date,
    TransactionType Type,
    Guid AccountId,
    decimal Amount,
    string Reason,
    string? Note,
    Guid? CategoryId,
    Guid? PaymentTypeId,
    IReadOnlyList<Guid>? TagIds);

public record UpdateTransactionRequest(
    DateOnly Date,
    TransactionType Type,
    Guid AccountId,
    decimal Amount,
    string Reason,
    string? Note,
    Guid? CategoryId,
    Guid? PaymentTypeId,
    IReadOnlyList<Guid>? TagIds);

public record CreateSelfTransferRequest(
    DateOnly Date,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Reason,
    string? Note,
    Guid? PaymentTypeId,
    IReadOnlyList<Guid>? TagIds);

public record TransactionFilters(
    DateOnly? From,
    DateOnly? To,
    Guid? AccountId,
    Guid? CategoryId,
    Guid? PaymentTypeId,
    Guid? TagId,
    TransactionType? Type,
    string? Search);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public record TransactionReportRow(
    DateOnly Date,
    string Type,
    string AccountName,
    decimal Amount,
    string Reason,
    string? Note,
    string? CategoryName,
    string? PaymentTypeName,
    string? TagsCsv);

public record TransactionReport(DateOnly From, DateOnly To, IReadOnlyList<TransactionReportRow> Rows);
