namespace HTMX.Application.Web.Pages.Components;

using System;
using System.Collections.Concurrent;
using System.Linq;

/// <summary>
/// The Long job page's pretend jobs: each takes <see cref="Steps"/> steps of a second, counted from when it started,
/// and does nothing else. The page is about asking how far a job has got, so a job's progress is only the time it has had.
/// </summary>
/// <remarks>
/// One instance serves the application, so a job started in one request is found by the next. It keeps the newest
/// jobs only, so that starting jobs over and over takes no more memory; a job it has let go of is no longer found.
/// </remarks>
public sealed class LongJobs
{
    /// <summary>
    /// How many steps a job takes.
    /// </summary>
    public const int Steps = 10;

    // The first job's number; job numbers are as wide as record identifiers, so a link to one is as easy to press.
    private const long FirstJob = 10001;

    // How many jobs are kept: the oldest goes when another starts.
    private const int Kept = 100;

    // How long a step takes.
    private static readonly TimeSpan StepLength = TimeSpan.FromSeconds(1);

    private readonly TimeProvider _time;

    // When each job started, by its number. Starting a job locks this, so that two never take the same number.
    private readonly ConcurrentDictionary<long, DateTimeOffset> _started = new ConcurrentDictionary<long, DateTimeOffset>();

    /// <summary>
    /// Initializes a new instance of the <see cref="LongJobs"/> class.
    /// </summary>
    /// <param name="time">The clock a job's steps are counted by.</param>
    public LongJobs(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        this._time = time;
    }

    /// <summary>
    /// Starts a job, now.
    /// </summary>
    /// <returns>The job's number, one more than the newest job's.</returns>
    public long Start()
    {
        lock (this._started)
        {
            long job = this._started.IsEmpty ? FirstJob : this._started.Keys.Max() + 1;
            this._started[job] = this._time.GetUtcNow();
            this.ForgetOldest();
            return job;
        }
    }

    /// <summary>
    /// Says how many of its steps a job has done.
    /// </summary>
    /// <param name="job">The job's number.</param>
    /// <returns>The steps done, from 0 to <see cref="Steps"/>, or <see langword="null"/> when no job has the
    /// number.</returns>
    public int? StepsDone(long job)
    {
        if (!this._started.TryGetValue(job, out DateTimeOffset started))
        {
            return null;
        }

        long done = (this._time.GetUtcNow() - started).Ticks / StepLength.Ticks;
        return (int)Math.Min(done, Steps);
    }

    /// <summary>
    /// Lets go of the oldest job once more than <see cref="Kept"/> are kept.
    /// </summary>
    private void ForgetOldest()
    {
        if (this._started.Count > Kept)
        {
            this._started.TryRemove(this._started.Keys.Min(), out _);
        }
    }
}
