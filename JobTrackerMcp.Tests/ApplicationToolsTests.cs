using JobTrackerMcp.Models;
using JobTrackerMcp.Tools;

namespace JobTrackerMcp.Tests
{
    // These tests run against the seeded JobTracker database (01_JobTracker_Setup.sql).
    // The JOBTRACKER_CONNECTION environment variable must be set.
    public class ApplicationToolsTests
    {
        [Fact]
        public async Task GetApplication_Id1_ReturnsContosoTech()
        {
            Application? app = await ApplicationTools.GetApplication(1);

            Assert.NotNull(app);
            Assert.Equal("Contoso Tech", app!.Company);
            Assert.Equal("Interview", app.Status);
        }

        [Fact]
        public async Task GetApplication_UnknownId_ReturnsNull()
        {
            Application? app = await ApplicationTools.GetApplication(999);

            Assert.Null(app);
        }

        [Fact]
        public async Task ListApplications_NoFilter_Returns15Rows()
        {
            List<Application> apps = await ApplicationTools.ListApplications();

            Assert.Equal(15, apps.Count);
        }

        [Fact]
        public async Task ListApplications_InterviewStatus_Returns3Rows()
        {
            List<Application> apps = await ApplicationTools.ListApplications("Interview");

            Assert.Equal(3, apps.Count);
            Assert.All(apps, a => Assert.Equal("Interview", a.Status));
        }

        [Fact]
        public async Task ListApplications_StatusIsCaseInsensitive()
        {
            List<Application> apps = await ApplicationTools.ListApplications("rejected");

            Assert.Equal(2, apps.Count);
        }

        [Fact]
        public async Task ListApplications_LimitOf2_Returns2Rows()
        {
            List<Application> apps = await ApplicationTools.ListApplications(null, 2);

            Assert.Equal(2, apps.Count);
        }

        [Fact]
        public async Task ListApplications_LimitOfZero_IsRaisedToOne()
        {
            List<Application> apps = await ApplicationTools.ListApplications(null, 0);

            Assert.Single(apps);
        }

        [Fact]
        public async Task ListApplications_UnknownStatus_Throws()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => ApplicationTools.ListApplications("Hired"));
        }

        [Fact]
        public async Task GetFollowupsDue_ReturnsOnlyOpenRowsDueTodayOrEarlier()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");

            List<Application> apps = await ApplicationTools.GetFollowupsDue();

            // The seed dates move with the day the script ran, so more rows
            // become due as days pass. Check the rule, not an exact count.
            Assert.All(apps, a =>
            {
                Assert.NotNull(a.FollowUpOn);
                Assert.True(string.CompareOrdinal(a.FollowUpOn, today) <= 0);
                Assert.NotEqual("Rejected", a.Status);
                Assert.NotEqual("Withdrawn", a.Status);
            });
        }

        [Fact]
        public async Task GetFollowupsDue_IncludesTheSixSeededDueRows()
        {
            string[] expected =
            {
                "Contoso Tech", "Northwind Systems", "Wingtip Software",
                "Woodgrove Bank", "Lucerne Publishing", "Trey Research"
            };

            List<Application> apps = await ApplicationTools.GetFollowupsDue();
            List<string> companies = apps.Select(a => a.Company).ToList();

            Assert.True(apps.Count >= 6);
            Assert.All(expected, name => Assert.Contains(name, companies));
        }
    }
}
