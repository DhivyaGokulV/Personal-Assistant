using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.Finance.Settings;

// Accounts
public record AccountDto(Guid Id, string Name, string? Description, decimal OpeningBalance, DateOnly OpeningDate, AccountStatus Status);
public record CreateAccountRequest(string Name, string? Description, decimal OpeningBalance, DateOnly OpeningDate, AccountStatus Status);
public record UpdateAccountRequest(string Name, string? Description, decimal OpeningBalance, DateOnly OpeningDate, AccountStatus Status);

// Categories
public record CategoryDto(Guid Id, string Name, CategoryType Type);
public record CreateCategoryRequest(string Name, CategoryType Type);
public record UpdateCategoryRequest(string Name, CategoryType Type);

// Payment types
public record PaymentTypeDto(Guid Id, string Name, string? Description);
public record CreatePaymentTypeRequest(string Name, string? Description);
public record UpdatePaymentTypeRequest(string Name, string? Description);

// Tags
public record TagDto(Guid Id, string Name, string? Description, string Color);
public record CreateTagRequest(string Name, string? Description, string Color);
public record UpdateTagRequest(string Name, string? Description, string Color);
