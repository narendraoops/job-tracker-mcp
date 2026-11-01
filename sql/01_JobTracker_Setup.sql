/* ============================================================
   JobTracker database for the MCP learning project
   Run in SSMS (or sqlcmd) while connected as an admin account.
   Safe to re-run: it rebuilds the Applications table each time.

   BEFORE YOU RUN: change the password in the CREATE LOGIN line.
   ============================================================ */

USE master;
GO

IF DB_ID(N'JobTracker') IS NULL
    CREATE DATABASE JobTracker;
GO

-- Read-only login that our MCP server will use (never an admin account)
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'mcp_reader')
    CREATE LOGIN mcp_reader WITH PASSWORD = N'db123!';
GO

USE JobTracker;
GO

IF OBJECT_ID(N'dbo.Applications', N'U') IS NOT NULL
    DROP TABLE dbo.Applications;
GO

CREATE TABLE dbo.Applications
(
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Company     NVARCHAR(100) NOT NULL,
    Role        NVARCHAR(150) NOT NULL,
    Status      NVARCHAR(30)  NOT NULL
                CONSTRAINT CK_Applications_Status
                CHECK (Status IN (N'Applied', N'Screening', N'Interview', N'Offer', N'Rejected', N'Withdrawn')),
    Location    NVARCHAR(100) NULL,
    Source      NVARCHAR(50)  NULL,
    AppliedOn   DATE          NOT NULL,
    FollowUpOn  DATE          NULL,
    Notes       NVARCHAR(500) NULL
);
GO

-- Dates are relative to today, so follow-up tests always have data.
DECLARE @t DATE = CAST(GETDATE() AS DATE);

INSERT INTO dbo.Applications (Company, Role, Status, Location, Source, AppliedOn, FollowUpOn, Notes) VALUES
 (N'Contoso Tech',        N'Senior .NET Tech Lead',                   N'Interview', N'Pune',      N'LinkedIn',     DATEADD(DAY,-12,@t), @t,                  N'Round 2 done. Waiting for feedback.'),
 (N'Northwind Systems',   N'AI Solutions Lead',                       N'Screening', N'Dubai',     N'Referral',     DATEADD(DAY,-9,@t),  DATEADD(DAY,-2,@t),  N'Recruiter call held. Asked for updated resume.'),
 (N'Fabrikam Cloud',      N'Azure .NET Architect',                    N'Applied',   N'Remote',    N'LinkedIn',     DATEADD(DAY,-5,@t),  DATEADD(DAY,2,@t),   N'Applied through company site.'),
 (N'Tailspin Labs',       N'.NET Developer Lead',                     N'Rejected',  N'Bengaluru', N'Naukri',       DATEADD(DAY,-30,@t), NULL,                N'Went with an internal candidate.'),
 (N'Adventure Works',     N'Technical Project Manager',               N'Offer',     N'Pune',      N'Referral',     DATEADD(DAY,-25,@t), DATEADD(DAY,1,@t),   N'Offer received. Reply due this week.'),
 (N'Wingtip Software',    N'Senior Full Stack Engineer (.NET, Angular)', N'Interview', N'Abu Dhabi', N'LinkedIn',  DATEADD(DAY,-14,@t), @t,                  N'Technical round on Friday.'),
 (N'Litware Inc',         N'AI Engineer (.NET)',                      N'Applied',   N'Remote',    N'Company site', DATEADD(DAY,-3,@t),  DATEADD(DAY,4,@t),   NULL),
 (N'Proseware',           N'Engineering Manager',                     N'Screening', N'Dubai',     N'Recruiter',    DATEADD(DAY,-8,@t),  DATEADD(DAY,1,@t),   N'Waiting for hiring manager call.'),
 (N'Woodgrove Bank',      N'Lead Developer, Core Banking',            N'Interview', N'Dubai',     N'Recruiter',    DATEADD(DAY,-18,@t), DATEADD(DAY,-1,@t),  N'Waiting on panel schedule.'),
 (N'Alpine Ski House',    N'Cloud Solutions Architect',               N'Withdrawn', N'Pune',      N'Naukri',       DATEADD(DAY,-20,@t), NULL,                N'Role changed to part time.'),
 (N'Margie''s Travel',    N'.NET Backend Lead',                       N'Applied',   N'Bengaluru', N'LinkedIn',     DATEADD(DAY,-2,@t),  DATEADD(DAY,5,@t),   NULL),
 (N'Coho Winery IT',      N'Azure DevOps Lead',                       N'Rejected',  N'Pune',      N'Naukri',       DATEADD(DAY,-40,@t), NULL,                N'Position closed.'),
 (N'Lucerne Publishing',  N'Senior .NET Developer',                   N'Screening', N'Remote',    N'LinkedIn',     DATEADD(DAY,-6,@t),  @t,                  N'Phone screen booked. Send thank-you note after.'),
 (N'Blue Yonder Airlines',N'Tech Lead, Booking Platform',             N'Applied',   N'Dubai',     N'Referral',     DATEADD(DAY,-1,@t),  DATEADD(DAY,7,@t),   NULL),
 (N'Trey Research',       N'AI Platform Engineer',                    N'Applied',   N'Remote',    N'Company site', DATEADD(DAY,-10,@t), DATEADD(DAY,-3,@t),  N'No reply yet. Send a follow-up email.');
GO

-- Read-only access for the MCP server
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'mcp_reader')
    CREATE USER mcp_reader FOR LOGIN mcp_reader;
GO
ALTER ROLE db_datareader ADD MEMBER mcp_reader;
DENY INSERT, UPDATE, DELETE, ALTER ON SCHEMA::dbo TO mcp_reader;
GO

-- Quick check: expect 15 rows, 3 due today, 3 overdue
SELECT COUNT(*)                                             AS TotalRows,
       SUM(CASE WHEN FollowUpOn = CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) AS DueToday,
       SUM(CASE WHEN FollowUpOn <  CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) AS Overdue
FROM dbo.Applications;
GO
