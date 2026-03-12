namespace Application.Common.Models;

/// <summary>
/// Represents pagination parameters for querying paginated collections.
/// Handlers clamp <see cref="PageSize"/> to a maximum of 100 to prevent resource exhaustion.
/// </summary>
public class PagedRequest
{
    private const int MaxPageSize = 100;
    private int _pageNumber = 1;
    private int _pageSize = 10;

    /// <summary>
    /// Gets or sets the page number (1-indexed). Default is 1.
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value > 0 ? value : 1;
    }

    /// <summary>
    /// Gets or sets the page size (items per page). Clamped to 1–100. Default is 10.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Max(1, Math.Min(value, MaxPageSize));
    }
}
