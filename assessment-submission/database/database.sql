CREATE DATABASE CourseInquiryDb;
GO

USE CourseInquiryDb;
GO

/* ----------------------------- COURSE TABLE ---------------------------- */
CREATE TABLE dbo.Course
(
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Course PRIMARY KEY,
    CourseName  NVARCHAR(200) NOT NULL
);
GO

/* ----------------------------- COURSE INQUIRIES ---------------------------- */
CREATE TABLE dbo.CourseInquiries
(
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CourseInquiries PRIMARY KEY,
    FirstName         NVARCHAR(100)  NOT NULL,
    LastName          NVARCHAR(100)  NOT NULL,
    Email             NVARCHAR(256)  NOT NULL,
    Phone             NVARCHAR(50)   NULL,

    CourseId          INT NOT NULL ,

    PreferredLocation NVARCHAR(200)  NULL,
    Message           NVARCHAR(2000) NULL,

    Status            NVARCHAR(20) NOT NULL
        CONSTRAINT DF_CourseInquiries_Status DEFAULT ('New'),

    CreatedDate       DATETIME2(7) NOT NULL
        CONSTRAINT DF_CourseInquiries_Created DEFAULT (SYSUTCDATETIME()),

    UpdatedDate       DATETIME2(7) NOT NULL
        CONSTRAINT DF_CourseInquiries_Updated DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT FK_CourseInquiries_Course
        FOREIGN KEY (CourseId) REFERENCES dbo.Course(Id),

    CONSTRAINT CK_CourseInquiries_Status
        CHECK (Status IN ('New','Contacted','Pending','Registered','Closed','Archived'))
);
GO


/* ----------------------------- CRM SYNC LOGS ---------------------------- */
CREATE TABLE dbo.CrmSyncLogs
(
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmSyncLogs PRIMARY KEY,
    InquiryId     INT NOT NULL,

    Status        NVARCHAR(20) NOT NULL
        CONSTRAINT DF_CrmSyncLogs_Status DEFAULT ('Pending'),

    Attempts      INT NOT NULL
        CONSTRAINT DF_CrmSyncLogs_Attempts DEFAULT (0),

    ExternalId    NVARCHAR(100) NULL,
    LastErrorCode NVARCHAR(100) NULL,

    CreatedDate   DATETIME2(7) NOT NULL
        CONSTRAINT DF_CrmSyncLogs_Created DEFAULT (SYSUTCDATETIME()),

    UpdatedDate   DATETIME2(7) NOT NULL
        CONSTRAINT DF_CrmSyncLogs_Updated DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT FK_CrmSyncLogs_Inquiry
        FOREIGN KEY (InquiryId) REFERENCES dbo.CourseInquiries(Id)
);
GO


/* ------------------------------ SAMPLE DATA ------------------------------ */

INSERT INTO dbo.Course
    (CourseName)
VALUES
    ('PILATES Level 1'),
    ('PILATES Level 2'),
    ('PILATES Level 3');
GO

INSERT INTO dbo.CourseInquiries
    (FirstName, LastName, Email, Phone, CourseId, PreferredLocation, Message, Status, CreatedDate, UpdatedDate)
VALUES
    ('Alice', 'Wong',   'alice.wong@example.com',   '416-555-0101', 1, 'Toronto',  'available check', 'New',
        DATEADD(DAY, -1, SYSUTCDATETIME()), DATEADD(DAY, -1, SYSUTCDATETIME())),

    ('Bruno', 'Silva',  'bruno.silva@example.com',  '647-555-0102', 1, 'Markham',  'cancel check', 'Contacted',
        DATEADD(DAY, -3, SYSUTCDATETIME()), DATEADD(DAY, -2, SYSUTCDATETIME())),

    ('Chloe', 'Martin', 'chloe.martin@example.com','992-221-1242', 2, 'Online', 'appoment check', 'Pending',
        DATEADD(DAY, -5, SYSUTCDATETIME()), DATEADD(DAY, -4, SYSUTCDATETIME())),

    ('David', 'Okafor', 'alice.wong@example.com', '905-555-0104', 2, 'Toronto', 'price check', 'Registered',
        DATEADD(DAY, -6, SYSUTCDATETIME()), DATEADD(DAY, -1, SYSUTCDATETIME())),

    ('Emma', 'Tremblay','emma.tremblay@example.com','514-555-0105', 3, 'Montreal', 'group size check', 'Closed',
        DATEADD(DAY, -10, SYSUTCDATETIME()), DATEADD(DAY, -8, SYSUTCDATETIME()));
GO



-- 1) Inquiries created in the last 7 days
SELECT *
FROM dbo.CourseInquiries
WHERE CreatedDate >= DATEADD(DAY, -7, SYSUTCDATETIME())
ORDER BY CreatedDate DESC;

-- 2) Count inquiries by status
SELECT Status, COUNT(*) AS InquiryCount
FROM dbo.CourseInquiries
GROUP BY Status
ORDER BY InquiryCount DESC;

-- 3) Find duplicate inquiries by email address
SELECT Email, COUNT(*) AS DuplicateCount
FROM dbo.CourseInquiries
GROUP BY Email
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;