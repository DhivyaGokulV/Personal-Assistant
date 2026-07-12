using PersonalAssistant.Application.AssetTracker.PreciousMetals;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.AssetTracker.Possessions;

public enum PossessionSort { Name, BuyingDate, BuyingPrice, Status, SellingDate }

public record PossessionQuery(
    string? Search,
    AssetStatus? Status,
    PossessionSort SortBy = PossessionSort.Name,
    AssetSortDirection SortDirection = AssetSortDirection.Asc,
    int Page = 1,
    int PageSize = 25);

public record JewelleryDto(
    Guid Id,
    string Name,
    string? Description,
    DateOnly BuyingDate,
    decimal BuyingPrice,
    decimal QuantityInGrams,
    AssetStatus Status,
    DateOnly? SellingDate,
    decimal? SellingPrice,
    string? SellingNote,
    string CurrencyCode);

public record SaveJewelleryRequest(
    string Name,
    string? Description,
    DateOnly BuyingDate,
    decimal BuyingPrice,
    decimal QuantityInGrams,
    AssetStatus? Status);

public record PersonalAssetDto(
    Guid Id,
    string Name,
    string? Description,
    DateOnly BuyingDate,
    decimal BuyingPrice,
    AssetStatus Status,
    DateOnly? SellingDate,
    decimal? SellingPrice,
    string? SellingNote,
    string CurrencyCode);

public record SavePersonalAssetRequest(
    string Name,
    string? Description,
    DateOnly BuyingDate,
    decimal BuyingPrice,
    AssetStatus? Status);

public record SellPossessionRequest(string? SellingNote, DateOnly SellingDate, decimal SellingPrice);
public record PossessionExportRequest(DateOnly From, DateOnly To, ReportFormat Format);
