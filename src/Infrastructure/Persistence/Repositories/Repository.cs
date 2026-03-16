using Application.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="IRepository{T}"/>.
/// Wraps a <see cref="DbSet{T}"/> from the shared <see cref="ApplicationDbContext"/>
/// scoped to the current HTTP request.
///
/// All instances within a single <see cref="EfUnitOfWork"/> share the same
/// <see cref="ApplicationDbContext"/> instance, so ChangeTracker state is visible
/// across repositories and is flushed together by <c>SaveChangesAsync</c>.
/// </summary>
/// <typeparam name="T">A domain entity deriving from <see cref="BaseEntity"/>.</typeparam>
internal sealed class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly DbSet<T> _set;

    /// <summary>
    /// Initializes a new instance of <see cref="Repository{T}"/> bound to the given context.
    /// </summary>
    public Repository(ApplicationDbContext context) => _set = context.Set<T>();

    /// <inheritdoc />
    public IQueryable<T> AsQueryable() => _set;

    /// <inheritdoc />
    public ValueTask<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _set.FindAsync([id], cancellationToken);

    /// <inheritdoc />
    public void Add(T entity) => _set.Add(entity);

    /// <inheritdoc />
    public void Update(T entity) => _set.Update(entity);

    /// <inheritdoc />
    public void Remove(T entity) => _set.Remove(entity);
}
