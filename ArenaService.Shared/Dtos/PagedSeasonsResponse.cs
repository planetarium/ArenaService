using Libplanet.Crypto;

namespace ArenaService.Shared.Dtos;

public class PagedSeasonsResponse
{
    public required List<SeasonResponse> Seasons { get; set; }
    public required int TotalCount { get; set; }
    public required int PageNumber { get; set; }
    public required int PageSize { get; set; }
    public required int TotalPages { get; set; }
    public required bool HasNextPage { get; set; }
    public required bool HasPreviousPage { get; set; }
} 