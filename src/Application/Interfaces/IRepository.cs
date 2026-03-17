using Domain.Common;

namespace Application.Interfaces;

/// <summary>
/// Generic repository abstraction for a single entity type.
///
/// Exposes <see cref="AsQueryable"/> as a composable <see cref="IQueryable{T}"/> so that
/// handlers can chain any LINQ operator (Where, Select, Include, AsNoTracking, etc.)
/// without forcing query-method proliferation in the repository itself. The
/// <see cref="IUnitOfWork"/> coordinates one or more repository operations in a single
/// call to <see cref="IUnitOfWork.SaveChangesAsync"/>.
///
/// Mutation methods (<see cref="Add"/>, <see cref="Update"/>, <see cref="Remove"/>)
/// track changes in EF Core's ChangeTracker but do NOT persist until
/// <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
/// </summary>
/// <typeparam name="T">
/// A domain entity that derives from <see cref="BaseEntity"/> and therefore has
/// a <see cref="Guid"/> primary key.
/// </typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Returns a composable query root for this entity type.
    /// Equivalent to <c>DbSet&lt;T&gt;</c> from which LINQ chains are started.
    /// Apply <c>.AsNoTracking()</c> for read-only queries to avoid tracking overhead.
    /// Implemented as a method rather than a property so that NSubstitute can mock
    /// the call reliably in unit tests (method interception is more reliable than
    /// property getter interception for generic interface return types).
    /// </summary>
    IQueryable<T> AsQueryable();

    /// <summary>
    /// Finds an entity by its primary key using the EF Core identity map (first-level cache).
    /// If the entity is already tracked in the current scope, no database round-trip occurs.
    /// Returns <c>null</c> if no matching entity exists or it is soft-deleted.
    /// </summary>
    /// <param name="id">The entity's primary key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins tracking <paramref name="entity"/> in the <c>Added</c> state.
    /// The entity will be inserted into the database on the next <see cref="IUnitOfWork.SaveChangesAsync"/> call.
    /// </summary>
    void Add(T entity);

    /// <summary>
    /// Begins tracking <paramref name="entity"/> in the <c>Modified</c> state.
    /// All scalar properties of the entity will be updated in the database on the next save.
    /// Prefer updating navigation properties via the tracked entity graph where possible.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Begins tracking <paramref name="entity"/> in the <c>Deleted</c> state.
    /// The entity will be hard-deleted from the database on the next save.
    /// For soft-delete, call <c>entity.Delete()</c> and then <see cref="Update"/> instead.
    /// </summary>
    void Remove(T entity);
}
