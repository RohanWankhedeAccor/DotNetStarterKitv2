-- Seed script for DotNetStarterKitV2
-- Run after migrations: dotnet ef database update
-- Then execute: sqlcmd -S TH-5CD41368G0\SQLEXPRESS -d DotNetStarterKitV2 -i seed.sql

USE DotNetStarterKitV2;
GO

-- Clear existing data (in reverse dependency order)
DELETE FROM [dbo].[UserRoles];
DELETE FROM [dbo].[Products];
DELETE FROM [dbo].[Projects];
DELETE FROM [dbo].[Users];
DELETE FROM [dbo].[Roles];
GO

-- SEED ROLES
INSERT INTO [dbo].[Roles] (
    [Name],
    [CreatedAt],
    [CreatedBy],
    [ModifiedAt],
    [ModifiedBy],
    [IsDeleted]
) VALUES
    ('Admin', GETUTCDATE(), 'system', GETUTCDATE(), 'system', 0),
    ('User', GETUTCDATE(), 'system', GETUTCDATE(), 'system', 0),
    ('Moderator', GETUTCDATE(), 'system', GETUTCDATE(), 'system', 0);

GO

-- SEED USERS
INSERT INTO [dbo].[Users] (
    [Email],
    [FullName],
    [PasswordHash],
    [Status],
    [CreatedAt],
    [CreatedBy],
    [ModifiedAt],
    [ModifiedBy],
    [IsDeleted]
) VALUES
    (
        'admin@example.com',
        'Admin User',
        'hashed_password_admin_123',
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    ),
    (
        'john.doe@example.com',
        'John Doe',
        'hashed_password_john_123',
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    ),
    (
        'jane.smith@example.com',
        'Jane Smith',
        'hashed_password_jane_123',
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    ),
    (
        'bob.wilson@example.com',
        'Bob Wilson',
        'hashed_password_bob_123',
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    );

GO

-- SEED USER ROLES
INSERT INTO [dbo].[UserRoles] (
    [UserId],
    [RoleId],
    [CreatedAt],
    [CreatedBy],
    [ModifiedAt],
    [ModifiedBy],
    [IsDeleted]
) VALUES
    -- Admin user gets Admin role
    (1, 1, GETUTCDATE(), 'system', GETUTCDATE(), 'system', 0),
    -- John gets User role
    (2, 2, GETUTCDATE(), 'system', GETUTCDATE(), 'system', 0),
    -- Jane gets User and Moderator roles
    (3, 2, GETUTCDATE(), 'system', GETUTCDATE(), 'system', 0),
    (3, 3, GETUTCDATE(), 'system', GETUTCDATE(), 'system', 0),
    -- Bob gets User role
    (4, 2, GETUTCDATE(), 'system', GETUTCDATE(), 'system', 0);

GO

-- SEED PRODUCTS
INSERT INTO [dbo].[Products] (
    [Name],
    [Description],
    [Price],
    [Status],
    [CreatedAt],
    [CreatedBy],
    [ModifiedAt],
    [ModifiedBy],
    [IsDeleted]
) VALUES
    (
        'Wireless Mouse',
        'Ergonomic wireless mouse with 2.4GHz connection',
        29.99,
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    ),
    (
        'USB-C Hub',
        '7-in-1 USB-C hub with HDMI, USB 3.0, and SD card reader',
        49.99,
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    ),
    (
        'Mechanical Keyboard',
        'RGB mechanical keyboard with Cherry MX switches',
        159.99,
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    ),
    (
        '4K Monitor',
        '27-inch 4K ultra HD monitor with 60Hz refresh rate',
        399.99,
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    ),
    (
        'Laptop Stand',
        'Adjustable aluminum laptop stand for better ergonomics',
        39.99,
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    ),
    (
        'Webcam HD',
        '1080p HD webcam with built-in microphone',
        79.99,
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    );

GO

-- SEED PROJECTS
INSERT INTO [dbo].[Projects] (
    [Name],
    [Description],
    [OwnerId],
    [Status],
    [CreatedAt],
    [CreatedBy],
    [ModifiedAt],
    [ModifiedBy],
    [IsDeleted]
) VALUES
    (
        'E-Commerce Platform',
        'Building a modern e-commerce platform with React and ASP.NET Core',
        1, -- Admin user
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    ),
    (
        'Mobile App Development',
        'Cross-platform mobile app using React Native',
        2, -- John Doe
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    ),
    (
        'Cloud Migration',
        'Migrating on-premise infrastructure to Azure',
        3, -- Jane Smith
        1, -- Completed
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    ),
    (
        'Analytics Dashboard',
        'Real-time analytics dashboard for business intelligence',
        1, -- Admin user
        0, -- Active
        GETUTCDATE(),
        'system',
        GETUTCDATE(),
        'system',
        0
    );

GO

-- Verify seeded data
PRINT 'Data seeding completed successfully!';
PRINT '';
PRINT 'Summary:';
SELECT 'Roles' as [Table], COUNT(*) as [Count] FROM [dbo].[Roles] WHERE IsDeleted = 0
UNION ALL
SELECT 'Users' as [Table], COUNT(*) as [Count] FROM [dbo].[Users] WHERE IsDeleted = 0
UNION ALL
SELECT 'UserRoles' as [Table], COUNT(*) as [Count] FROM [dbo].[UserRoles] WHERE IsDeleted = 0
UNION ALL
SELECT 'Products' as [Table], COUNT(*) as [Count] FROM [dbo].[Products] WHERE IsDeleted = 0
UNION ALL
SELECT 'Projects' as [Table], COUNT(*) as [Count] FROM [dbo].[Projects] WHERE IsDeleted = 0;

GO
