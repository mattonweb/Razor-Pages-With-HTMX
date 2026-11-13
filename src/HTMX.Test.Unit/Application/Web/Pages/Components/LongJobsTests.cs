namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Pages.Components;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="LongJobs"/> class: the Long job page's pretend
/// jobs, whose progress is the time they have had.
/// </summary>
[TestClass]
public sealed class LongJobsTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that jobs are numbered from 10001, each one more than the newest.
    /// </summary>
    [TestMethod]
    public void Start_TwoJobs_NumbersThemFrom10001()
    {
        // Arrange (Given)
        LongJobs jobs = new LongJobs(new FakeTimeProvider());

        // Act (When)
        long first = jobs.Start();
        long second = jobs.Start();

        // Assert (Then)
        Assert.AreEqual(
            (10001L, 10002L),
            (first, second),
            "Jobs should be numbered from 10001, in the order they start.");
    }

    /// <summary>
    /// Unit test to verify that a job has done a step for each whole second since it started, and no more than its
    /// steps.
    /// </summary>
    /// <param name="seconds">How long after it started the job is asked about.</param>
    /// <param name="expected">The steps it should have done.</param>
    [TestMethod]
    [DataRow(0.0, 0, DisplayName = "Just started")]
    [DataRow(3.5, 3, DisplayName = "Three and a half seconds in")]
    [DataRow(10.0, 10, DisplayName = "Ten seconds in")]
    [DataRow(60.0, 10, DisplayName = "Long since finished")]
    public void StepsDone_TimeSinceStarted_CountsWholeSeconds(double seconds, int expected)
    {
        // Arrange (Given)
        FakeTimeProvider clock = new FakeTimeProvider();
        LongJobs jobs = new LongJobs(clock);
        long job = jobs.Start();

        // Act (When)
        clock.Advance(TimeSpan.FromSeconds(seconds));
        int? done = jobs.StepsDone(job);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            done,
            "A job should have done a step a second, up to its last.");
    }

    /// <summary>
    /// Unit test to verify that a number no job has, or a job let go of once a hundred newer ones started, is not
    /// found.
    /// </summary>
    [TestMethod]
    public void StepsDone_UnknownOrForgottenJob_IsNull()
    {
        // Arrange (Given)
        LongJobs jobs = new LongJobs(new FakeTimeProvider());
        long oldest = jobs.Start();
        for (int started = 1; started <= 100; started++)
        {
            jobs.Start();
        }

        // Act (When)
        int? unknown = jobs.StepsDone(99999);
        int? forgotten = jobs.StepsDone(oldest);

        // Assert (Then)
        Assert.AreEqual(
            ((int?)null, (int?)null),
            (unknown, forgotten),
            "A number no job has, and the oldest job once a hundred newer started, should not be found.");
    }

    #endregion Public Methods
}
