IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028190807_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [Identification] nvarchar(30) NOT NULL,
        [FirstName] nvarchar(80) NOT NULL,
        [LastName] nvarchar(80) NOT NULL,
        [Email] nvarchar(150) NULL,
        [Phone] nvarchar(30) NULL,
        [BirthDate] date NULL,
        [HireDate] date NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028190807_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_Identification] ON [Employees] ([Identification]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028190807_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028190807_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028201715_AddContractsAndSalaryHistory'
)
BEGIN
    CREATE TABLE [Contracts] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [ContractType] nvarchar(30) NOT NULL,
        [Position] nvarchar(100) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Contracts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Contracts_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028201715_AddContractsAndSalaryHistory'
)
BEGIN
    CREATE TABLE [SalaryHistories] (
        [Id] int NOT NULL IDENTITY,
        [ContractId] int NOT NULL,
        [MonthlySalary] decimal(18,2) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [Reason] nvarchar(250) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_SalaryHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalaryHistories_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028201715_AddContractsAndSalaryHistory'
)
BEGIN
    CREATE INDEX [IX_Contracts_EmployeeId_StartDate] ON [Contracts] ([EmployeeId], [StartDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028201715_AddContractsAndSalaryHistory'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalaryHistories_ContractId_EffectiveFrom] ON [SalaryHistories] ([ContractId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028201715_AddContractsAndSalaryHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028201715_AddContractsAndSalaryHistory', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028202539_AddContractsAndSalaryHistory-2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028202539_AddContractsAndSalaryHistory-2', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029180610_AddPayrollNovelties'
)
BEGIN
    CREATE TABLE [PayrollNovelties] (
        [Id] int NOT NULL IDENTITY,
        [ContractId] int NOT NULL,
        [PeriodYear] int NOT NULL,
        [PeriodMonth] int NOT NULL,
        [NoveltyDate] date NOT NULL,
        [NoveltyType] nvarchar(40) NOT NULL,
        [Category] nvarchar(20) NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [Quantity] decimal(18,4) NULL,
        [UnitValue] decimal(18,2) NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PayrollNovelties] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollNovelties_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029180610_AddPayrollNovelties'
)
BEGIN
    CREATE INDEX [IX_PayrollNovelties_ContractId_PeriodYear_PeriodMonth] ON [PayrollNovelties] ([ContractId], [PeriodYear], [PeriodMonth]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029180610_AddPayrollNovelties'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029180610_AddPayrollNovelties', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [EmployeeId] int NULL,
        [MustChangePassword] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUsers_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AspNetUsers_EmployeeId] ON [AspNetUsers] ([EmployeeId]) WHERE [EmployeeId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105173944_AddIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105173944_AddIdentity', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163234_AddPayrollProcesing'
)
BEGIN
    CREATE TABLE [PayrollPeriods] (
        [Id] int NOT NULL IDENTITY,
        [Year] int NOT NULL,
        [Month] int NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ClosedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PayrollPeriods] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163234_AddPayrollProcesing'
)
BEGIN
    CREATE TABLE [PayrollDetails] (
        [Id] int NOT NULL IDENTITY,
        [PayrollPeriodId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [ContractId] int NOT NULL,
        [Identification] nvarchar(30) NOT NULL,
        [EmployeeName] nvarchar(161) NOT NULL,
        [Position] nvarchar(100) NOT NULL,
        [MonthlySalary] decimal(18,2) NOT NULL,
        [TotalEarnings] decimal(18,2) NOT NULL,
        [TotalDeductions] decimal(18,2) NOT NULL,
        [NetPay] decimal(18,2) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_PayrollDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollDetails_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PayrollDetails_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PayrollDetails_PayrollPeriods_PayrollPeriodId] FOREIGN KEY ([PayrollPeriodId]) REFERENCES [PayrollPeriods] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163234_AddPayrollProcesing'
)
BEGIN
    CREATE TABLE [PayrollConcepts] (
        [Id] int NOT NULL IDENTITY,
        [PayrollDetailId] int NOT NULL,
        [Code] nvarchar(40) NOT NULL,
        [Description] nvarchar(120) NOT NULL,
        [Category] nvarchar(20) NOT NULL,
        [Quantity] decimal(18,2) NULL,
        [UnitValue] decimal(18,2) NULL,
        [Amount] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_PayrollConcepts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollConcepts_PayrollDetails_PayrollDetailId] FOREIGN KEY ([PayrollDetailId]) REFERENCES [PayrollDetails] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163234_AddPayrollProcesing'
)
BEGIN
    CREATE INDEX [IX_PayrollConcepts_PayrollDetailId] ON [PayrollConcepts] ([PayrollDetailId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163234_AddPayrollProcesing'
)
BEGIN
    CREATE INDEX [IX_PayrollDetails_ContractId] ON [PayrollDetails] ([ContractId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163234_AddPayrollProcesing'
)
BEGIN
    CREATE INDEX [IX_PayrollDetails_EmployeeId] ON [PayrollDetails] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163234_AddPayrollProcesing'
)
BEGIN
    CREATE INDEX [IX_PayrollDetails_PayrollPeriodId] ON [PayrollDetails] ([PayrollPeriodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163234_AddPayrollProcesing'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106163234_AddPayrollProcesing', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163749_AddPayrollProcesing-2'
)
BEGIN
    ALTER TABLE [PayrollDetails] DROP CONSTRAINT [FK_PayrollDetails_Contracts_ContractId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163749_AddPayrollProcesing-2'
)
BEGIN
    ALTER TABLE [PayrollDetails] DROP CONSTRAINT [FK_PayrollDetails_Employees_EmployeeId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163749_AddPayrollProcesing-2'
)
BEGIN
    ALTER TABLE [PayrollDetails] DROP CONSTRAINT [FK_PayrollDetails_PayrollPeriods_PayrollPeriodId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163749_AddPayrollProcesing-2'
)
BEGIN
    DROP INDEX [IX_PayrollDetails_PayrollPeriodId] ON [PayrollDetails];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163749_AddPayrollProcesing-2'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PayrollConcepts]') AND [c].[name] = N'Quantity');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [PayrollConcepts] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [PayrollConcepts] ALTER COLUMN [Quantity] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163749_AddPayrollProcesing-2'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PayrollPeriods_Year_Month] ON [PayrollPeriods] ([Year], [Month]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163749_AddPayrollProcesing-2'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PayrollDetails_PayrollPeriodId_EmployeeId] ON [PayrollDetails] ([PayrollPeriodId], [EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163749_AddPayrollProcesing-2'
)
BEGIN
    ALTER TABLE [PayrollDetails] ADD CONSTRAINT [FK_PayrollDetails_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163749_AddPayrollProcesing-2'
)
BEGIN
    ALTER TABLE [PayrollDetails] ADD CONSTRAINT [FK_PayrollDetails_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163749_AddPayrollProcesing-2'
)
BEGIN
    ALTER TABLE [PayrollDetails] ADD CONSTRAINT [FK_PayrollDetails_PayrollPeriods_PayrollPeriodId] FOREIGN KEY ([PayrollPeriodId]) REFERENCES [PayrollPeriods] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106163749_AddPayrollProcesing-2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106163749_AddPayrollProcesing-2', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109183437_AddBonusSettlement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261109183437_AddBonusSettlement', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109185135_AddBonusSettlements'
)
BEGIN
    CREATE TABLE [BonusSettlements] (
        [Id] int NOT NULL IDENTITY,
        [Year] int NOT NULL,
        [Semester] int NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_BonusSettlements] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109185135_AddBonusSettlements'
)
BEGIN
    CREATE TABLE [BonusSettlementDetails] (
        [Id] int NOT NULL IDENTITY,
        [BonusSettlementId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [ContractId] int NOT NULL,
        [Identification] nvarchar(30) NOT NULL,
        [EmployeeName] nvarchar(180) NOT NULL,
        [WorkedDays] int NOT NULL,
        [SalaryAccrued] decimal(18,2) NOT NULL,
        [TransportAccrued] decimal(18,2) NOT NULL,
        [BonusAmount] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_BonusSettlementDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BonusSettlementDetails_BonusSettlements_BonusSettlementId] FOREIGN KEY ([BonusSettlementId]) REFERENCES [BonusSettlements] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109185135_AddBonusSettlements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BonusSettlementDetails_BonusSettlementId_ContractId] ON [BonusSettlementDetails] ([BonusSettlementId], [ContractId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109185135_AddBonusSettlements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BonusSettlements_Year_Semester] ON [BonusSettlements] ([Year], [Semester]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109185135_AddBonusSettlements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261109185135_AddBonusSettlements', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109194626_AddSalaryClassificationToNovelties'
)
BEGIN
    ALTER TABLE [PayrollNovelties] ADD [IsSalaryConstitutive] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109194626_AddSalaryClassificationToNovelties'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261109194626_AddSalaryClassificationToNovelties', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109195749_AddSalaryNoveltiesToBonusDetails'
)
BEGIN
    ALTER TABLE [BonusSettlementDetails] ADD [SalaryNovelties] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109195749_AddSalaryNoveltiesToBonusDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261109195749_AddSalaryNoveltiesToBonusDetails', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110161548_AddBonusPreviousPayments'
)
BEGIN
    CREATE TABLE [BonusPreviousPayments] (
        [Id] int NOT NULL IDENTITY,
        [BonusSettlementId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentDate] date NOT NULL,
        [Reference] nvarchar(100) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_BonusPreviousPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BonusPreviousPayments_BonusSettlements_BonusSettlementId] FOREIGN KEY ([BonusSettlementId]) REFERENCES [BonusSettlements] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110161548_AddBonusPreviousPayments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BonusPreviousPayments_BonusSettlementId_EmployeeId_Reference] ON [BonusPreviousPayments] ([BonusSettlementId], [EmployeeId], [Reference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110161548_AddBonusPreviousPayments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261110161548_AddBonusPreviousPayments', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111184654_IntegrateBonusIntoPayroll'
)
BEGIN
    CREATE TABLE [BonusPayrollAllocations] (
        [Id] int NOT NULL IDENTITY,
        [BonusSettlementDetailId] int NOT NULL,
        [PayrollPeriodId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [IncludedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_BonusPayrollAllocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BonusPayrollAllocations_BonusSettlementDetails_BonusSettlementDetailId] FOREIGN KEY ([BonusSettlementDetailId]) REFERENCES [BonusSettlementDetails] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BonusPayrollAllocations_PayrollPeriods_PayrollPeriodId] FOREIGN KEY ([PayrollPeriodId]) REFERENCES [PayrollPeriods] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111184654_IntegrateBonusIntoPayroll'
)
BEGIN
    CREATE TABLE [PayslipIssues] (
        [Id] int NOT NULL IDENTITY,
        [PayrollDetailId] int NOT NULL,
        [IssuedAtUtc] datetime2 NOT NULL,
        [IssuedByUserId] nvarchar(max) NULL,
        [IncludesBonus] bit NOT NULL,
        CONSTRAINT [PK_PayslipIssues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayslipIssues_PayrollDetails_PayrollDetailId] FOREIGN KEY ([PayrollDetailId]) REFERENCES [PayrollDetails] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111184654_IntegrateBonusIntoPayroll'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BonusPayrollAllocations_BonusSettlementDetailId] ON [BonusPayrollAllocations] ([BonusSettlementDetailId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111184654_IntegrateBonusIntoPayroll'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BonusPayrollAllocations_PayrollPeriodId_EmployeeId] ON [BonusPayrollAllocations] ([PayrollPeriodId], [EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111184654_IntegrateBonusIntoPayroll'
)
BEGIN
    CREATE INDEX [IX_PayslipIssues_PayrollDetailId] ON [PayslipIssues] ([PayrollDetailId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111184654_IntegrateBonusIntoPayroll'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111184654_IntegrateBonusIntoPayroll', N'10.0.14');
END;

COMMIT;
GO

