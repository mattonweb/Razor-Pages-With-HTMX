namespace HTMX.Test.Unit.Application.Web.Authentication;

using System;
using System.Security.Claims;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="SignedInUser"/> class: reading who is signed in
/// from a request's user.
/// </summary>
[TestClass]
public sealed class SignedInUserTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the signed-in user's identifier is read from the name-identifier claim.
    /// </summary>
    [TestMethod]
    public void Of_NameIdentifierClaim_ReturnsTheUser()
    {
        // Arrange (Given)
        ClaimsPrincipal user = CreateUser(new Claim(ClaimTypes.NameIdentifier, "10001"));

        // Act (When)
        UserId id = SignedInUser.Of(user);

        // Assert (Then)
        Assert.AreEqual(
            new UserId(10001),
            id,
            "The identifier should come from the name-identifier claim.");
    }

    /// <summary>
    /// Unit test to verify that a request with no user's identifier fails at once, rather than saving a change that
    /// names no one.
    /// </summary>
    /// <param name="claim">The name-identifier claim's value, or <see langword="null"/> for no claim.</param>
    [TestMethod]
    [DataRow(null, DisplayName = "No claim")]
    [DataRow("pat@example.com", DisplayName = "Not a number")]
    public void Of_NoUserIdentifier_Throws(string? claim)
    {
        // Arrange (Given)
        ClaimsPrincipal user = claim is null
            ? CreateUser()
            : CreateUser(new Claim(ClaimTypes.NameIdentifier, claim));

        // Act (When)
        InvalidOperationException thrown = Assert.ThrowsExactly<InvalidOperationException>(
            () => SignedInUser.Of(user),
            "A request with no user's identifier should be refused.");

        // Assert (Then)
        Assert.IsNotNull(
            thrown.Message,
            "The exception should say what is missing.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a signed-in user with the claims given.
    /// </summary>
    /// <param name="claims">The user's claims.</param>
    /// <returns>The user.</returns>
    private static ClaimsPrincipal CreateUser(params Claim[] claims)
    {
        ClaimsIdentity identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    #endregion Private Methods
}
