using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Application.Interfaces;

namespace Integration.Audit;

/// <summary>
/// Verifies that <see cref="ApplicationDbContext.SaveChangesAsync"/> automatically
/// creates <see cref="AuditLog"/> entries whenever a <see cref="Domain.Common.BaseEntity"/>
/// is added, modified, or soft-deleted.
///
/// Uses an isolated SQLite in-memory database per test class — same approach as
/// <see cref="CustomWebApplicationFactory"/> — so these tests run without SQL Server.
/// </summary>
public sealed class AuditCaptureTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly DateTimeOffset _fixedNow = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    public AuditCaptureTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserIdString.Returns("test-user-id");

        var dateTime = Substitute.For<IDateTimeService>();
        dateTime.Now.Returns(_fixedNow);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options, currentUser, dateTime);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_CreatesCreatedAuditEntry()
    {
        var user = new User("audit@example.com", "Audit", "User", "hash");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var log = await _context.AuditLogs.SingleAsync();
        log.Action.Should().Be(AuditAction.Created);
        log.TableName.Should().Be("Users");
        log.EntityId.Should().Be(user.Id.ToString());
        log.ChangedBy.Should().Be("test-user-id");
        log.ChangedAt.Should().Be(_fixedNow);
        log.OldValues.Should().BeNull("new entities have no prior state");
        log.NewValues.Should().NotBeNullOrEmpty("new entity values must be captured");
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityModified_CreatesUpdatedAuditEntry()
    {
        var user = new User("update@example.com", "Before", "Update", "hash");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Perform a scalar modification.
        user.UpdateName("After", "Update");
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        // Two saves = two audit entries; filter to the Updated one.
        var log = await _context.AuditLogs
            .Where(a => a.Action == AuditAction.Updated)
            .SingleAsync();

        log.EntityId.Should().Be(user.Id.ToString());
        log.OldValues.Should().NotBeNullOrEmpty("prior values must be captured for updates");
        log.NewValues.Should().NotBeNullOrEmpty("new values must be captured for updates");
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntitySoftDeleted_CreatesDeletedAuditEntry()
    {
        var user = new User("delete@example.com", "To", "Delete", "hash");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        user.Delete();  // Soft-delete via BaseEntity.Delete() — sets IsDeleted = true
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        var log = await _context.AuditLogs
            .Where(a => a.Action == AuditAction.Deleted)
            .SingleAsync();

        log.EntityId.Should().Be(user.Id.ToString());
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityRestored_CreatesRestoredAuditEntry()
    {
        // Seed a soft-deleted user.
        var user = new User("restore@example.com", "To", "Restore", "hash");
        user.Delete();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Restore it.
        user.Restore();
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        var log = await _context.AuditLogs
            .Where(a => a.Action == AuditAction.Restored)
            .SingleAsync();

        log.EntityId.Should().Be(user.Id.ToString());
    }

    [Fact]
    public async Task SaveChangesAsync_DoesNotRecursivelyAuditAuditLogs()
    {
        // Adding two entities in one save must produce exactly two AuditLog entries —
        // not four (which would indicate AuditLog entries are themselves being audited).
        var user1 = new User("a@example.com", "A", "User", "hash");
        var user2 = new User("b@example.com", "B", "User", "hash");

        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var count = await _context.AuditLogs.CountAsync();
        count.Should().Be(2, "one AuditLog per entity — AuditLog entries must not be recursively audited");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
