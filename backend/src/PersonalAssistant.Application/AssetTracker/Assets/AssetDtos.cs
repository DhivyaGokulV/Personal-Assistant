using PersonalAssistant.Application.AssetTracker.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.AssetTracker.Assets;

public record AssetGroupDto(
    Guid Id,
    string Name,
    string? Description,
    AssetTagBadge? Tag,
    int AssetCount,
    decimal TotalCurrentValue);

public record CreateAssetGroupRequest(string Name, string? Description, Guid? TagId);
public record UpdateAssetGroupRequest(string Name, string? Description, Guid? TagId);

public record AssetDto(
    Guid Id,
    Guid GroupId,
    string GroupName,
    string Name,
    string? Description,
    AssetTagBadge? Tag,
    DateOnly? BuyingDate,
    decimal? BuyingPrice,
    DateOnly? SellingDate,
    decimal? SellingPrice,
    AssetStatus Status,
    decimal? CurrentPrice,
    DateOnly? LastPriceAsOf);

public record AssetPriceHistoryDto(Guid Id, DateOnly AsOf, decimal Price, string? Note);

public record AssetWithHistoryDto(
    AssetDto Asset,
    IReadOnlyList<AssetPriceHistoryDto> History);

public record CreateAssetRequest(
    Guid GroupId,
    string Name,
    string? Description,
    Guid? TagId,
    DateOnly? BuyingDate,
    decimal? BuyingPrice,
    DateOnly? SellingDate,
    decimal? SellingPrice,
    AssetStatus Status,
    decimal CurrentPrice);

public record UpdateAssetRequest(
    Guid GroupId,
    string Name,
    string? Description,
    Guid? TagId,
    DateOnly? BuyingDate,
    decimal? BuyingPrice,
    DateOnly? SellingDate,
    decimal? SellingPrice,
    AssetStatus Status);

public record AddAssetPriceRequest(DateOnly AsOf, decimal Price, string? Note);
public record UpdateAssetPriceRequest(DateOnly AsOf, decimal Price, string? Note);

public record AssetReportRow(
    string GroupName,
    string Name,
    string? TagName,
    string Status,
    DateOnly? BuyingDate,
    decimal? BuyingPrice,
    DateOnly? SellingDate,
    decimal? SellingPrice,
    decimal? CurrentPrice);

public record AssetReport(IReadOnlyList<AssetReportRow> Rows);
