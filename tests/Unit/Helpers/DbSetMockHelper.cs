using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Unit.Helpers;

/// <summary>
/// Creates NSubstitute DbSet mocks backed by a List, supporting EF Core async LINQ operators.
/// </summary>
internal static class DbSetMockHelper
{
    public static DbSet<T> Create<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = Substitute.For<DbSet<T>, IQueryable<T>, IAsyncEnumerable<T>>();

        ((IQueryable<T>)mockSet).Provider
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        ((IQueryable<T>)mockSet).Expression.Returns(queryable.Expression);
        ((IQueryable<T>)mockSet).ElementType.Returns(queryable.ElementType);
        ((IQueryable<T>)mockSet).GetEnumerator().Returns(queryable.GetEnumerator());
        ((IAsyncEnumerable<T>)mockSet).GetAsyncEnumerator(Arg.Any<CancellationToken>())
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        return mockSet;
    }
}
