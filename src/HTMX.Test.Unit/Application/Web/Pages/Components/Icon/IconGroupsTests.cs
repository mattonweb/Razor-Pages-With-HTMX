namespace HTMX.Test.Unit.Application.Web.Pages.Components.Icon;

using System.Linq;
using HTMX.Application.Web.Pages.Components.Icon;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="IconGroups"/> class: the icons the Icon page shows,
/// grouped as the sprite groups them.
/// </summary>
[TestClass]
public sealed class IconGroupsTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that each icon is in one group only, once, so that the page draws each icon once.
    /// </summary>
    [TestMethod]
    public void All_EveryGroup_NamesEachIconOnce()
    {
        // Arrange (Given)
        string[] names = IconGroups.All.SelectMany(group => group.Names).ToArray();

        // Act (When)
        int distinct = names.Distinct().Count();

        // Assert (Then)
        Assert.AreEqual(
            names.Length,
            distinct,
            "No icon should be named twice: " + string.Join(", ", names));
    }

    #endregion Public Methods
}
