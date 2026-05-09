using PersonalAssistant.Application.AssetTracker.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.AssetTracker.Liabilities;

public record LiabilityDto(
    Guid Id,
    string Name,
    string? Description,
    AssetTagBadge? Tag,
    LiabilityStatus Status,
    decimal CurrentAmount,
    DateOnly? LastUpdate);

public record LiabilityHistoryDto(
    Guid Id,
    DateOnly Date,
    LiabilityTxType Type,
    decimal Amount,
    decimal RunningBalance,
    string? Note);

public record LiabilityDetailDto(LiabilityDto Liability, IReadOnlyList<LiabilityHistoryDto> History);

/// <summary>
/// On creation, an opening Acquisition history entry is added equal to InitialAmount.
/// </summary>
public record CreateLiabilityRequest(string Name, string? Description, Guid? TagId, decimal InitialAmount, DateOnly? Date, string? Note);
public record UpdateLiabilityRequest(string Name, string? Description, Guid? TagId);

public record AddLiabilityEntryRequest(DateOnly Date, LiabilityTxType Type, decimal Amount, string? Note);
public record UpdateLiabilityEntryRequest(DateOnly Date, LiabilityTxType Type, decimal Amount, string? Note);

public record LiabilityReportRow(string Name, string? TagName, string Status, decimal CurrentAmount, DateOnly? LastUpdate);
public record LiabilityReport(IReadOnlyList<LiabilityReportRow> Rows);
