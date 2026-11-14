namespace HTMX.Test.Integration;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright;

/// <summary>
/// One browser test's browser: Microsoft Edge as installed on this machine, headless, driven by Playwright, with one
/// page on the application, 800 by 600, and every error its console reports kept for the test to check.
/// </summary>
/// <remarks>
/// The installed Edge is used rather than a browser Playwright downloads, so the tests need Edge on the machine that
/// runs them and nothing else. A Content Security Policy violation is reported to the console as an error, as is a
/// script's own error, so a test that finds the console clean has met neither.
/// </remarks>
internal sealed class BrowserSession : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;
    private readonly List<string> _consoleErrors;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowserSession"/> class.
    /// </summary>
    /// <param name="playwright">The Playwright driver.</param>
    /// <param name="browser">The browser it launched.</param>
    /// <param name="page">The page the test drives.</param>
    /// <param name="consoleErrors">The errors the page's console reports, as they come.</param>
    private BrowserSession(IPlaywright playwright, IBrowser browser, IPage page, List<string> consoleErrors)
    {
        this._playwright = playwright;
        this._browser = browser;
        this.Page = page;
        this._consoleErrors = consoleErrors;
    }

    /// <summary>
    /// Gets the page the test drives. Addresses it goes to are relative to the application's.
    /// </summary>
    public IPage Page { get; }

    /// <summary>
    /// Gets the errors the page's console has reported so far.
    /// </summary>
    public string[] ConsoleErrors
    {
        get
        {
            lock (this._consoleErrors)
            {
                return [.. this._consoleErrors];
            }
        }
    }

    /// <summary>
    /// Opens Edge on the application, running as much script as the test asks for.
    /// </summary>
    /// <param name="application">The application's address, from
    /// <see cref="ApplicationFactory.StartServerForBrowser"/>.</param>
    /// <param name="scripting">How much script the page runs.</param>
    /// <returns>The session. The test disposes it.</returns>
    public static async Task<BrowserSession> OpenAsync(Uri application, Scripting scripting)
    {
        ArgumentNullException.ThrowIfNull(application);
        IPlaywright playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        BrowserTypeLaunchOptions launch = new BrowserTypeLaunchOptions { Channel = "msedge", Headless = true };
        IBrowser browser = await playwright.Chromium.LaunchAsync(launch).ConfigureAwait(false);
        BrowserNewContextOptions options = new BrowserNewContextOptions
        {
            BaseURL = application.ToString(),
            ViewportSize = new ViewportSize { Width = 800, Height = 600 },
            JavaScriptEnabled = scripting != Scripting.Off,
        };
        IBrowserContext context = await browser.NewContextAsync(options).ConfigureAwait(false);

        // A wait that finds nothing fails the test in five seconds, not Playwright's thirty.
        context.SetDefaultTimeout(5000);

        // The application names no icon, so the browser asks for /favicon.ico on its own, and the 404 it gets would be
        // the console's one error on every page. The browser gets no content instead, so that the console stays a
        // check on what the pages themselves do.
        await context.RouteAsync("**/favicon.ico", static route => route.FulfillAsync(NoIcon())).ConfigureAwait(false);
        if (scripting == Scripting.WithoutHtmx)
        {
            await context.RouteAsync("**/js/htmx.min.*.js", static route => route.FulfillAsync(EmptyScript()))
                .ConfigureAwait(false);
        }

        IPage page = await context.NewPageAsync().ConfigureAwait(false);
        List<string> consoleErrors = [];
        page.Console += (sender, message) => Keep(consoleErrors, message);
        return new BrowserSession(playwright, browser, page, consoleErrors);
    }

    /// <summary>
    /// Closes the browser and the Playwright driver.
    /// </summary>
    /// <returns>A task that completes when both have closed.</returns>
    public async ValueTask DisposeAsync()
    {
        await this._browser.CloseAsync().ConfigureAwait(false);
        this._playwright.Dispose();
    }

    /// <summary>
    /// Makes the answer that stands in for HTMX's file when the page runs without it: an empty script, which loads
    /// without an error and defines nothing.
    /// </summary>
    /// <returns>The answer.</returns>
    private static RouteFulfillOptions EmptyScript()
    {
        return new RouteFulfillOptions { Status = 200, ContentType = "text/javascript", Body = string.Empty };
    }

    /// <summary>
    /// Makes the answer to the browser's own request for an icon: no content.
    /// </summary>
    /// <returns>The answer.</returns>
    private static RouteFulfillOptions NoIcon()
    {
        return new RouteFulfillOptions { Status = 204 };
    }

    /// <summary>
    /// Keeps a console message when it is an error.
    /// </summary>
    /// <param name="consoleErrors">The errors kept.</param>
    /// <param name="message">The message.</param>
    private static void Keep(List<string> consoleErrors, IConsoleMessage message)
    {
        if (message.Type != "error")
        {
            return;
        }

        lock (consoleErrors)
        {
            consoleErrors.Add(message.Text);
        }
    }
}
