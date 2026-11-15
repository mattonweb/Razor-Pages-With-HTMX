namespace HTMX.Application.Web.Pages.Components;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The Load more page, at /components/load-more: the active organizations by name, ten at first, and a Load more
/// button that adds the next ten under them, up to a hundred.
/// </summary>
/// <remarks>
/// The list grows only when Load more is pressed (<see cref="GrowingListModel"/>), and the first new row's link takes
/// the keyboard.
/// </remarks>
public sealed class LoadMoreModel : GrowingListModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadMoreModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads the organizations.</param>
    public LoadMoreModel(IOrganizationService organizations)
        : base(organizations)
    {
    }

    /// <inheritdoc/>
    protected override string PageName => "./LoadMore";

    /// <inheritdoc/>
    protected override string AnswerPartial => "_LoadMoreAnswer";

    /// <inheritdoc/>
    protected override bool GrowsOnScroll => false;
}
