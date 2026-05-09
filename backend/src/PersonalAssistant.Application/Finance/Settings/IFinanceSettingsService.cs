namespace PersonalAssistant.Application.Finance.Settings;

public interface IFinanceSettingsService
{
    // Accounts
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(bool includeInactive, CancellationToken ct);
    Task<AccountDto> CreateAccountAsync(CreateAccountRequest req, CancellationToken ct);
    Task<AccountDto> UpdateAccountAsync(Guid id, UpdateAccountRequest req, CancellationToken ct);
    Task DeleteAccountAsync(Guid id, CancellationToken ct);

    // Categories
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest req, CancellationToken ct);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest req, CancellationToken ct);
    Task DeleteCategoryAsync(Guid id, CancellationToken ct);

    // Payment types
    Task<IReadOnlyList<PaymentTypeDto>> GetPaymentTypesAsync(CancellationToken ct);
    Task<PaymentTypeDto> CreatePaymentTypeAsync(CreatePaymentTypeRequest req, CancellationToken ct);
    Task<PaymentTypeDto> UpdatePaymentTypeAsync(Guid id, UpdatePaymentTypeRequest req, CancellationToken ct);
    Task DeletePaymentTypeAsync(Guid id, CancellationToken ct);

    // Tags
    Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken ct);
    Task<TagDto> CreateTagAsync(CreateTagRequest req, CancellationToken ct);
    Task<TagDto> UpdateTagAsync(Guid id, UpdateTagRequest req, CancellationToken ct);
    Task DeleteTagAsync(Guid id, CancellationToken ct);
}
