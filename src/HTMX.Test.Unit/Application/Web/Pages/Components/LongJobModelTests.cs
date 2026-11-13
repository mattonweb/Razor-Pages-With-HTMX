namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="LongJobModel"/> class: the Long job page, whose
/// card polls while its job runs and stops once it has finished.
/// </summary>
[TestClass]
public sealed class LongJobModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that HTMX's poll for a running job gets the card alone, still running, and that the poll
    /// that finds the job finished gets the card with the toast that says so.
    /// </summary>
    /// <param name="seconds">How long after the job started the poll comes.</param>
    /// <param name="partial">The partial the poll should get.</param>
    /// <param name="running">Whether the card should still be running, and so polling.</param>
    [TestMethod]
    [DataRow(4.0, "_LongJobCard", true, DisplayName = "Running")]
    [DataRow(10.0, "_LongJobFinished", false, DisplayName = "Finished")]
    public void OnGet_PollByHtmx_AnswersTheCardUntilTheJobFinishes(double seconds, string partial, bool running)
    {
        // Arrange (Given)
        FakeTimeProvider clock = new FakeTimeProvider();
        LongJobs jobs = new LongJobs(clock);
        long job = jobs.Start();
        clock.Advance(TimeSpan.FromSeconds(seconds));
        LongJobModel page = CreatePage(jobs, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = page.OnGet(job);

        // Assert (Then)
        Assert.AreEqual(
            (partial, running, !running),
            ((result as PartialViewResult)?.ViewName, page.Running, page.Toast is not null),
            "The poll should get the card while the job runs, and the card and a toast once it has finished.");
    }

    /// <summary>
    /// Unit test to verify that a number no job has gets the page saying so, with 404.
    /// </summary>
    [TestMethod]
    public void OnGet_UnknownJob_Answers404()
    {
        // Arrange (Given)
        LongJobModel page = CreatePage(new LongJobs(new FakeTimeProvider()), HtmxRequest.None);

        // Act (When)
        IActionResult result = page.OnGet(99999);

        // Assert (Then)
        Assert.AreEqual(
            (true, true, StatusCodes.Status404NotFound),
            (result is PageResult, page.Missing, page.Response.StatusCode),
            "An unknown job should get the page, which says so, with 404.");
    }

    /// <summary>
    /// Unit test to verify that Start without HTMX redirects to the new job's address, so that refreshing it starts
    /// nothing, and with HTMX answers the running card and pushes that address into the history.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="redirected">Whether the answer should be a redirect.</param>
    /// <param name="pushed">The address HTMX should push, or empty for none.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.None), true, "", DisplayName = "Without HTMX")]
    [DataRow(nameof(HtmxRequest.Partial), false, "/Components/LongJob?job=10001", DisplayName = "With HTMX")]
    public void OnPost_EachKindOfRequest_StartsTheJobAndShowsIt(string kind, bool redirected, string pushed)
    {
        // Arrange (Given)
        LongJobModel page = CreatePage(new LongJobs(new FakeTimeProvider()), Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        IActionResult result = page.OnPost();
        IHeaderDictionary headers = page.Response.Headers;

        // Assert (Then)
        Assert.AreEqual(
            (redirected, !redirected, pushed),
            (result is RedirectToPageResult, result is PartialViewResult, headers["HX-Push-Url"].ToString()),
            "Start should redirect to the job, or answer HTMX with its card and push its address.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the page as the framework would for one request: the headers HTMX 4 sends for that kind of request,
    /// and stand-ins for routing and what a partial needs to render.
    /// </summary>
    /// <param name="jobs">The jobs the page starts and reads.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static LongJobModel CreatePage(LongJobs jobs, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Components/LongJob");
        return new LongJobModel(jobs)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
        };
    }

    #endregion Private Methods
}
