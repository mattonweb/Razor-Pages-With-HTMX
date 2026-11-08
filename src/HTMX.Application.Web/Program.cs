namespace HTMX.Application.Web;

using HTMX.Adapter.Customers;
using HTMX.Adapter.Customers.Configuration;
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
    /// Registers Razor Pages and the customers adapter, which owns the database.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    private static void AddServices(IServiceCollection services)
    {
        services.AddRazorPages();

        CustomersOptions customers = new CustomersOptions
        {
            DatabaseName = CustomersDatabaseName,
        };
        services.AddCustomersAdapter(customers);
    }

    /// <summary>
    /// Builds the request pipeline: error handling, HTTPS, static files and the Razor Pages.
    /// </summary>
    /// <param name="application">The application being built.</param>
    private static void UsePipeline(WebApplication application)
    {
        if (!application.Environment.IsDevelopment())
        {
            application.UseExceptionHandler("/Error");
            application.UseHsts();
        }

        application.UseHttpsRedirection();
        application.MapStaticAssets();

        PageActionEndpointConventionBuilder pages = application.MapRazorPages();
        pages.WithStaticAssets();
    }
}
