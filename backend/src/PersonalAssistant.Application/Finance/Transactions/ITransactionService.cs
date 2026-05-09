namespace PersonalAssistant.Application.Finance.Transactions;

public interface ITransactionService
{
    Task<PagedResult<TransactionDto>> ListAsync(TransactionFilters filters, int page, int pageSize, CancellationToken ct);
    Task<TransactionDto> GetAsync(Guid id, CancellationToken ct);
    Task<TransactionDto> CreateAsync(CreateTransactionRequest req, CancellationToken ct);
    Task<TransactionDto> UpdateAsync(Guid id, UpdateTransactionRequest req, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<TransactionDto>> CreateSelfTransferAsync(CreateSelfTransferRequest req, CancellationToken ct);

    Task<TransactionReport> GetReportAsync(DateOnly from, DateOnly to, Guid? accountId, CancellationToken ct);
}
