namespace HTMX.Test.Unit.Application.Web.Authentication;

using System.Security.Claims;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="DemoAuthenticationHandler"/> class, registered as
/// the application registers it.
/// </summary>
[TestClass]
public sealed class DemoAuthenticationHandlerTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that any request is signed in as Demo User, whose identifier a page reads with
    /// <see cref="SignedInUser"/>.
    /// </summary>
    [TestMethod]
    public async Task AuthenticateAsync_AnyRequest_SignsInDemoUser()
    {
        // Arrange (Given)
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        AuthenticationBuilder authentication = services.AddAuthentication(DemoAuthenticationHandler.SchemeName);
        authentication.AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>(
            DemoAuthenticationHandler.SchemeName,
            configureOptions: null);
        using (ServiceProvider provider = services.BuildServiceProvider())
        {
            DefaultHttpContext http = new DefaultHttpContext { RequestServices = provider };

            // Act (When)
            AuthenticateResult result = await http.AuthenticateAsync().ConfigureAwait(false);

            // Assert (Then)
            Assert.IsTrue(
                result.Succeeded,
                "Every request should be signed in.");
            ClaimsPrincipal? user = result.Principal;
            Assert.IsNotNull(
                user,
                "The request should have a user.");
            Assert.AreEqual(
                new UserId(10001),
                SignedInUser.Of(user),
                "The signed-in user should be Demo User.");
            Assert.AreEqual(
                "Demo User",
                user.Identity?.Name,
                "The user's name should be Demo User's.");
        }
    }

    #endregion Public Methods
}
