namespace HTMX.Test.Unit.Application.Web.Authorization;

using System.Security.Claims;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="PublishingHandler"/> class: the Publishing policy's
/// requirement, met for a user who may publish.
/// </summary>
[TestClass]
public sealed class PublishingHandlerTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the requirement is met for a signed-in user who may publish.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task HandleAsync_UserWhoMayPublish_MeetsTheRequirement()
    {
        // Arrange (Given)
        PublishingHandler handler = new PublishingHandler(new Publishers());
        AuthorizationHandlerContext context = CreateContext(SignedIn("10001"));

        // Act (When)
        await handler.HandleAsync(context).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsTrue(
            context.HasSucceeded,
            "A user who may publish should meet the requirement.");
    }

    /// <summary>
    /// Unit test to verify that the requirement is left unmet for a user whose permission was withdrawn, so that a page
    /// drawn before the change is refused on its next request.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task HandleAsync_UserWithdrawn_LeavesItUnmet()
    {
        // Arrange (Given)
        Publishers publishers = new Publishers();
        publishers.Withdraw(new UserId(10001));
        PublishingHandler handler = new PublishingHandler(publishers);
        AuthorizationHandlerContext context = CreateContext(SignedIn("10001"));

        // Act (When)
        await handler.HandleAsync(context).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsFalse(
            context.HasSucceeded,
            "A user whose permission was withdrawn should not meet the requirement.");
    }

    /// <summary>
    /// Unit test to verify that a request with no signed-in user leaves the requirement unmet rather than failing.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task HandleAsync_NoSignedInUser_LeavesItUnmet()
    {
        // Arrange (Given)
        PublishingHandler handler = new PublishingHandler(new Publishers());
        AuthorizationHandlerContext context = CreateContext(new ClaimsPrincipal(new ClaimsIdentity()));

        // Act (When)
        await handler.HandleAsync(context).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsFalse(
            context.HasSucceeded,
            "A request with no signed-in user should not meet the requirement.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates what the authorization service hands a handler: the Publishing requirement, for a user.
    /// </summary>
    /// <param name="user">The request's user.</param>
    /// <returns>The context.</returns>
    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user)
    {
        return new AuthorizationHandlerContext([new PublishingRequirement()], user, resource: null);
    }

    /// <summary>
    /// Creates a signed-in user with an identifier.
    /// </summary>
    /// <param name="id">The user's identifier, as the sign-in writes it in the name-identifier claim.</param>
    /// <returns>The user.</returns>
    private static ClaimsPrincipal SignedIn(string id)
    {
        Claim[] claims = [new Claim(ClaimTypes.NameIdentifier, id)];
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    #endregion Private Methods
}
