namespace HTMX.Application.Web;

using HTMX.Adapter.Customers;
using HTMX.Adapter.Customers.Configuration;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// The application's entry point and composition root: it registers the services and builds the request pipeline.
/// </summary>
public static class Program
{
    /// <summary>
    /// The name of the customers adapter's in-memory database, which is created each time the application starts.
    /// </summary>
    private static readonly DatabaseName CustomersDatabaseName = new DatabaseName("Customers");

    /// <summary>
    /// Starts the application and runs it until it is stopped.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        AddServices(builder.Services);

        WebApplication application = builder.Build();
        UsePipeline(application);
        application.Run();
    }

    /// <summary>
    /// Registers Razor Pages, with every URL they write in lower case, the sign-in scheme, and the customers adapter,
    /// which owns the database.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    private static void AddServices(IServiceCollection services)
    {
        services.AddRazorPages();

        // Links and form actions come out as /organizations, not /Organizations; a request matches either way.
        // Query string values keep their case (LowercaseQueryStrings stays off), so a filter keeps its capitals.
        services.AddRouting(static options => options.LowercaseUrls = true);

        // Every request is signed in as Demo User, so a page reads who made a change from its User, as it would behind
        // a real sign-in.
        AuthenticationBuilder authentication = services.AddAuthentication(DemoAuthenticationHandler.SchemeName);
        authentication.AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>(
            DemoAuthenticationHandler.SchemeName,
            configureOptions: null);

        CustomersOptions customers = new CustomersOptions
        {
            DatabaseName = CustomersDatabaseName,
        };
        services.AddCustomersAdapter(customers);
    }

    /// <summary>
    /// Builds the request pipeline: error handling, HTTPS, static files, sign-in and the Razor Pages.
    /// </summary>
    /// <param name="application">The application being built.</param>
    private static void UsePipeline(WebApplication application)
    {
        if (!application.Environment.IsDevelopment())
        {
            application.UseExceptionHandler("/error");
            application.UseHsts();
        }

        application.UseHttpsRedirection();
        application.MapStaticAssets();
        application.UseAuthentication();

        PageActionEndpointConventionBuilder pages = application.MapRazorPages();
        pages.WithStaticAssets();
    }
}
