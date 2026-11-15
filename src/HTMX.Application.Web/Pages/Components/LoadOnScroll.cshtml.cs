namespace HTMX.Application.Web.Pages.Components;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The Load on scroll page, at /components/load-on-scroll: the active organizations by name, ten at first, growing ten
/// at a time as the last row comes into view, up to a hundred, with a Load more link above the list as well.
/// </summary>
/// <remarks>
/// The last row asks for the next ten as it comes into view, and its rows take no keyboard, since the user did not ask
/// for them; they come after the row the keyboard may be on, so Tab goes on into them. A status line says what came.
/// Load more, pressed, adds ten as on the Load more page (<see cref="GrowingListModel"/>), and its first new row takes
/// the keyboard. Nothing follows the list, which would move further away with every load.
/// </remarks>
public sealed class LoadOnScrollModel : GrowingListModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadOnScrollModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads the organizations.</param>
    public LoadOnScrollModel(IOrganizationService organizations)
        : base(organizations)
    {
    }

    /// <inheritdoc/>
    protected override string PageName => "./LoadOnScroll";

    /// <inheritdoc/>
    protected override string AnswerPartial => "_LoadOnScrollAnswer";

    /// <inheritdoc/>
    protected override bool GrowsOnScroll => true;
}
