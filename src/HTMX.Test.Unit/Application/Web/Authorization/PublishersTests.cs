namespace HTMX.Test.Unit.Application.Web.Authorization;

using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="Publishers"/> class: who may publish the Not
/// allowed page's articles.
/// </summary>
[TestClass]
public sealed class PublishersTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that Demo User may publish at first, and no other user may.
    /// </summary>
    /// <param name="user">The user's identifier.</param>
    /// <param name="may">Whether the user should be able to publish.</param>
    [TestMethod]
    [DataRow(10001L, true, DisplayName = "Demo User")]
    [DataRow(10002L, false, DisplayName = "Another user")]
    public void May_AtFirst_OnlyDemoUser(long user, bool may)
    {
        // Arrange (Given)
        Publishers publishers = new Publishers();

        // Act (When)
        bool answer = publishers.May(new UserId(user));

        // Assert (Then)
        Assert.AreEqual(
            may,
            answer,
            "Only Demo User should be able to publish at first.");
    }

    /// <summary>
    /// Unit test to verify that a withdrawn user may no longer publish, and that a grant lets them again.
    /// </summary>
    [TestMethod]
    public void Withdraw_ThenGrant_StopsTheUserThenLetsThemAgain()
    {
        // Arrange (Given)
        Publishers publishers = new Publishers();
        UserId demoUser = new UserId(10001);

        // Act (When)
        publishers.Withdraw(demoUser);
        bool withdrawn = publishers.May(demoUser);
        publishers.Grant(demoUser);
        bool granted = publishers.May(demoUser);

        // Assert (Then)
        Assert.AreEqual(
            (false, true),
            (withdrawn, granted),
            "The user should be stopped, then let publish again.");
    }

    /// <summary>
    /// Unit test to verify that a grant lets a user publish who could not, and leaves Demo User as they were.
    /// </summary>
    [TestMethod]
    public void Grant_AnotherUser_LetsThemPublishToo()
    {
        // Arrange (Given)
        Publishers publishers = new Publishers();

        // Act (When)
        publishers.Grant(new UserId(10002));

        // Assert (Then)
        Assert.AreEqual(
            (true, true),
            (publishers.May(new UserId(10002)), publishers.May(new UserId(10001))),
            "Both users should be able to publish.");
    }

    #endregion Public Methods
}
