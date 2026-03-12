namespace Application.Common.Models;

/// <summary>
/// Represents a paginated response containing items and pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// Gets or sets the collection of items in this page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-indexed).
    /// </summary>
    public required int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public required int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public required int TotalCount { get; set; }

    /// <summary>
    /// Gets the total number of pages (calculated).
    /// </summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

    /// <summary>
    /// Gets a value indicating whether there are more pages after the current page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Gets a value indicating whether there are pages before the current page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
}
