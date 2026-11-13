namespace HTMX.Application.Web.Pages.Components;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The Long job page, at /components/long-job: a pretend job that takes ten seconds, whose card asks the server how far
/// it has got every second until it has finished, and then stops asking.
/// </summary>
/// <remarks>
/// The job's number is in the address, <c>?job=10001</c>, which loads the card as it stands, so the address holds the
/// view. While the job runs, the card says <c>hx-get</c> with its own address and <c>hx-trigger="every 1s"</c>, and
/// each answer is the card again; the answer for a finished job leaves both out, so the polling stops, and brings a
/// toast. Without HTMX the card's Check again link loads the page, as it stands then.
/// </remarks>
public sealed class LongJobModel : PageModel
{
    // The card, which the page draws and an HTMX request gets alone, and the answer to the poll that finds the job
    // finished: the card and a toast.
    private const string CardPartial = "_LongJobCard";
    private const string FinishedPartial = "_LongJobFinished";

    private readonly LongJobs _jobs;

    /// <summary>
    /// Initializes a new instance of the <see cref="LongJobModel"/> class.
    /// </summary>
    /// <param name="jobs">The jobs, which start one and say how far one has got.</param>
    public LongJobModel(LongJobs jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        this._jobs = jobs;
    }

    /// <summary>
    /// Gets the number of the job the card shows, or <see langword="null"/> before one is started.
    /// </summary>
    public long? Job { get; private set; }

    /// <summary>
    /// Gets how many steps the job has done, or <see langword="null"/> when no job is shown or none has its number.
    /// </summary>
    public int? StepsDone { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the job is still running, so the card asks again in a second.
    /// </summary>
    public bool Running => this.StepsDone < LongJobs.Steps;

    /// <summary>
    /// Gets a value indicating whether the address names a job that does not exist, or no longer does.
    /// </summary>
    public bool Missing => this.Job is not null && this.StepsDone is null;

    /// <summary>
    /// Gets a value indicating whether the page's heading takes the focus: when HTMX loaded the whole page.
    /// </summary>
    public bool HeadingFocused { get; private set; }

    /// <summary>
    /// Gets the toast that says the job finished, which the poll that finds it finished brings, or
    /// <see langword="null"/>.
    /// </summary>
    public Toast? Toast { get; private set; }

    /// <summary>
    /// Shows the card for the job the address names, or the Start button when it names none. HTMX's poll gets the card
    /// alone, and with it, once the job has finished, the toast that says so; anything else gets the whole page. A
    /// number no job has answers 404, with the card saying so.
    /// </summary>
    /// <param name="job">The job's number, from the query string, if any.</param>
    /// <returns>The card or the page.</returns>
    public IActionResult OnGet(long? job)
    {
        this.HeadingFocused = HtmxHeaders.IsHtmxRequest(this.Request);
        this.Show(job);
        if (this.Missing)
        {
            this.Response.StatusCode = StatusCodes.Status404NotFound;
        }

        if (!HtmxHeaders.IsPartialRequest(this.Request))
        {
            return this.Page();
        }

        if (this.Running || this.StepsDone is null)
        {
            return this.Partial(CardPartial, this);
        }

        this.Toast = new Toast { Message = $"Job {this.Job} has finished." };
        return this.Partial(FinishedPartial, this);
    }

    /// <summary>
    /// Starts a job. Without HTMX the browser is redirected to the job's address, so that refreshing it does not start
    /// another; with HTMX the answer is the card, polling, and the job's address goes into the history.
    /// </summary>
    /// <returns>The card, or the redirect.</returns>
    public IActionResult OnPost()
    {
        long job = this._jobs.Start();
        if (!HtmxHeaders.IsPartialRequest(this.Request))
        {
            return this.RedirectToPage(new { job });
        }

        string address = this.Url.Page("./LongJob", new { job })
            ?? throw new InvalidOperationException("The Long job page has no address.");
        HtmxHeaders.PushAddress(this.Response, address);
        this.Show(job);
        return this.Partial(CardPartial, this);
    }

    /// <summary>
    /// Reads how far a job has got, for the card.
    /// </summary>
    /// <param name="job">The job's number, or <see langword="null"/> for none.</param>
    private void Show(long? job)
    {
        this.Job = job;
        this.StepsDone = job is long number ? this._jobs.StepsDone(number) : null;
    }
}
