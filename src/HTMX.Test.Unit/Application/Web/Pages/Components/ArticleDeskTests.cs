namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using HTMX.Application.Web.Pages.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ArticleDesk"/> class: the Not allowed page's
/// articles, each published or a draft.
/// </summary>
[TestClass]
public sealed class ArticleDeskTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the desk starts with its three articles in order, the first published.
    /// </summary>
    [TestMethod]
    public void All_AtFirst_ListsTheArticlesInOrder()
    {
        // Arrange (Given)
        ArticleDesk desk = new ArticleDesk();

        // Act (When)
        Article[] articles = desk.All();

        // Assert (Then)
        Assert.AreEqual(
            (3, 10001L, true, 10003L, false),
            (articles.Length, articles[0].Number, articles[0].Published, articles[2].Number, articles[2].Published),
            "The desk should list its articles in order, the first published and the others drafts.");
    }

    /// <summary>
    /// Unit test to verify that publishing a draft publishes that article alone.
    /// </summary>
    [TestMethod]
    public void Publish_ADraft_PublishesItAlone()
    {
        // Arrange (Given)
        ArticleDesk desk = new ArticleDesk();

        // Act (When)
        Article? published = desk.Publish(10002);
        Article[] articles = desk.All();

        // Assert (Then)
        Assert.AreEqual(
            (true, true, false),
            (published?.Published, articles[1].Published, articles[2].Published),
            "The article should be published, and the others left as they were.");
    }

    /// <summary>
    /// Unit test to verify that unpublishing a published article makes it a draft again.
    /// </summary>
    [TestMethod]
    public void Unpublish_APublishedArticle_MakesItADraft()
    {
        // Arrange (Given)
        ArticleDesk desk = new ArticleDesk();

        // Act (When)
        Article? unpublished = desk.Unpublish(10001);

        // Assert (Then)
        Assert.AreEqual(
            (false, false),
            (unpublished?.Published, desk.All()[0].Published),
            "The article should be a draft again.");
    }

    /// <summary>
    /// Unit test to verify that an article the desk does not have is neither published nor unpublished, and each says
    /// so.
    /// </summary>
    [TestMethod]
    public void PublishAndUnpublish_UnknownArticle_ReturnNull()
    {
        // Arrange (Given)
        ArticleDesk desk = new ArticleDesk();

        // Act (When)
        Article? published = desk.Publish(99);
        Article? unpublished = desk.Unpublish(99);

        // Assert (Then)
        Assert.AreEqual(
            (null, null),
            (published, unpublished),
            "An article the desk does not have should be neither published nor unpublished.");
    }

    #endregion Public Methods
}
