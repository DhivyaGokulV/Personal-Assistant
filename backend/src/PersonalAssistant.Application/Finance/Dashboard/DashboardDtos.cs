namespace PersonalAssistant.Application.Finance.Dashboard;

public record AccountStandingDto(
    Guid AccountId,
    string AccountName,
    decimal StartingStanding,
    decimal CurrentStanding,
    decimal Delta);

public record GroupStat(string Key, decimal Debits, decimal Credits, decimal Net);

public record DashboardView(
    DateOnly From,
    DateOnly To,
    decimal TotalStartingStanding,
    decimal TotalCurrentStanding,
    decimal TotalDebits,
    decimal TotalCredits,
    IReadOnlyList<AccountStandingDto> Accounts,
    IReadOnlyList<GroupStat> ByCategory,
    IReadOnlyList<GroupStat> ByAccount,
    IReadOnlyList<GroupStat> ByPaymentType,
    IReadOnlyList<GroupStat> ByTag);
