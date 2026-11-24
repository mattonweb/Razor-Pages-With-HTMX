namespace HTMX.Test.Unit.Application.Web.TagHelpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HTMX.Application.Web.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ComponentTagHelper"/> class and the components
/// drawn with it: that an attribute with no value draws the component itself and a value one of its parts, that each
/// component gives a part its own utilities, that a part a component does not have is refused, that the view's class
/// is kept and the attribute taken away, and that an attribute Razor left out draws nothing.
/// </summary>
[TestClass]
public sealed class ComponentTagHelperTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that each component gives the part its attribute names that part's own utilities, and gives
    /// the component itself its utilities when the attribute has no value.
    /// </summary>
    /// <param name="name">The component's attribute.</param>
    /// <param name="part">
    /// The part the attribute names, or <see langword="null"/> for an attribute with no value.
    /// </param>
    /// <param name="utility">A utility that belongs to that part alone.</param>
    [TestMethod]
    [DataRow("alert", "warning", "bg-warning-subtle", DisplayName = "Alert, warning tone")]
    [DataRow("alert", "content", "flex-[1_1_280px]", DisplayName = "Alert, content")]
    [DataRow("breadcrumbs", null, "[&_li+li]:before:content-['/']", DisplayName = "Breadcrumbs")]
    [DataRow("busy-bar", null, "in-busy:visible", DisplayName = "Busy bar")]
    [DataRow("busy-bar", null, "forced-colors:before:bg-[color:Highlight]", DisplayName = "Busy bar, in forced colors")]
    [DataRow("card", "header", "border-b", DisplayName = "Card, header")]
    [DataRow("dialog", "confirm", "w-[min(420px,calc(100vw_-_2_*_var(--space-4)))]", DisplayName = "Dialog, confirm")]
    [DataRow("dialog", "icon-warning", "bg-warning-subtle", DisplayName = "Dialog, warning icon")]
    [DataRow("empty-state", null, "[&>svg]:size-10", DisplayName = "Empty state")]
    [DataRow("empty-state", null, "[&>:is(h2,h3)]:text-lg", DisplayName = "Empty state, its heading at any level")]
    [DataRow("field", null, "[&>legend]:mb-1", DisplayName = "Field, its legend")]
    [DataRow("field", "message", "empty:hidden", DisplayName = "Field, message")]
    [DataRow("field-rows", null, "[&>li]:flex", DisplayName = "Rows")]
    [DataRow("field", "summary", "[&.validation-summary-valid]:hidden", DisplayName = "Field, summary")]
    [DataRow("fields", null, "gap-x-6", DisplayName = "Fields")]
    [DataRow("filter-box", null, "[&>input]:pl-9", DisplayName = "Filter box")]
    [DataRow("filter-box", null, "min-w-0", DisplayName = "Filter box, narrower than its input and Apply")]
    [DataRow("form-actions", "end", "ml-auto", DisplayName = "Form action bar, end")]
    [DataRow("grid", "number", "tabular-nums", DisplayName = "Grid, number")]
    [DataRow("grid", "archived", "[--grid-title:var(--text-muted)]", DisplayName = "Grid, archived row")]
    [DataRow("grid", "select", "pr-0", DisplayName = "Grid, selection column")]
    [DataRow("grid", "scroll", "relative", DisplayName = "Grid, scroll box, which places its hidden labels")]
    [DataRow("hero", "art", "@max-card:size-[120px]", DisplayName = "Hero, illustration")]
    [DataRow("menu", "danger", "text-danger", DisplayName = "Menu, danger item")]
    [DataRow("page-header", "title-row", "justify-between", DisplayName = "Page header, title row")]
    [DataRow("pager", null, "border-t", DisplayName = "Pager, at the foot")]
    [DataRow("pager", "head", "border-b", DisplayName = "Pager, at the head")]
    [DataRow("pager", "pages", "@max-card:mt-3", DisplayName = "Pager, pages")]
    [DataRow("pager", "size", "justify-self-end", DisplayName = "Pager, size")]
    [DataRow("related", "archived", "text-fg-muted", DisplayName = "Related-record link, archived")]
    [DataRow("related", null, "no-underline", DisplayName = "Related-record link, not underlined in a sentence")]
    [DataRow("relation", null, "[&>button]:[border-width:0_0_0_1px]", DisplayName = "Relation field's box")]
    [DataRow("segmented", "segment", "aria-[current=page]:bg-surface", DisplayName = "Segmented control, segment")]
    [DataRow(
        "segmented",
        "segment",
        "forced-colors:aria-[current=page]:forced-color-adjust-none",
        DisplayName = "Segmented control, segment in forced colors")]
    [DataRow("segmented", null, "flex-wrap", DisplayName = "Segmented control, wrapping on a narrow space")]
    [DataRow("segmented", "segment", "grow", DisplayName = "Segmented control, segment sharing its line")]
    [DataRow("tabs", "tab", "aria-[current=page]:text-accent", DisplayName = "Tabs, tab")]
    [DataRow(
        "tabs",
        "tab",
        "forced-colors:aria-[current=page]:border-[color:Highlight]",
        DisplayName = "Tabs, tab in forced colors")]
    [DataRow("tabs", "count", "forced-colors:border", DisplayName = "Tabs, count in forced colors")]
    [DataRow("toolbar", null, "[&>nav]:ml-auto", DisplayName = "Toolbar")]
    [DataRow("version", "current", "bg-brand", DisplayName = "Version badge, current")]
    [DataRow("version", null, "forced-colors:border", DisplayName = "Version badge, in forced colors")]
    [DataRow(
        "version",
        "current",
        "forced-colors:forced-color-adjust-none",
        DisplayName = "Version badge, current in forced colors")]
    public void Process_PartNamed_AddsThePartsUtilities(string name, string? part, string utility)
    {
        // Arrange (Given)
        TagHelperAttribute attribute = part is null
            ? new TagHelperAttribute(name)
            : new TagHelperAttribute(name, new HtmlString(part));

        // Act (When)
        TagHelperOutput output = Process(CreateHelper(name), attribute);
        bool added = ClassesOf(output).Contains(utility);

        // Assert (Then)
        Assert.IsTrue(
            added,
            $"The part should have the utility {utility}.");
    }

    /// <summary>
    /// Unit test to verify that a part a component does not have is refused rather than drawn as some other part.
    /// </summary>
    /// <param name="name">The component's attribute.</param>
    [TestMethod]
    [DataRow("alert", DisplayName = "Alert")]
    [DataRow("breadcrumbs", DisplayName = "Breadcrumbs")]
    [DataRow("busy-bar", DisplayName = "Busy bar")]
    [DataRow("card", DisplayName = "Card")]
    [DataRow("dialog", DisplayName = "Dialog")]
    [DataRow("empty-state", DisplayName = "Empty state")]
    [DataRow("field", DisplayName = "Field")]
    [DataRow("fields", DisplayName = "Fields")]
    [DataRow("field-rows", DisplayName = "Rows")]
    [DataRow("filter-box", DisplayName = "Filter box")]
    [DataRow("form-actions", DisplayName = "Form action bar")]
    [DataRow("grid", DisplayName = "Grid")]
    [DataRow("hero", DisplayName = "Hero")]
    [DataRow("menu", DisplayName = "Menu")]
    [DataRow("page-header", DisplayName = "Page header")]
    [DataRow("pager", DisplayName = "Pager")]
    [DataRow("related", DisplayName = "Related-record link")]
    [DataRow("relation", DisplayName = "Relation field's box")]
    [DataRow("segmented", DisplayName = "Segmented control")]
    [DataRow("tabs", DisplayName = "Tabs")]
    [DataRow("toolbar", DisplayName = "Toolbar")]
    [DataRow("version", DisplayName = "Version badge")]
    public void Process_UnknownPart_ThrowsInvalidOperationException(string name)
    {
        // Arrange (Given)
        ComponentTagHelper helper = CreateHelper(name);
        TagHelperAttribute attribute = new TagHelperAttribute(name, new HtmlString("sidebar"));
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            Process(helper, attribute);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A part the component does not have should be refused.");
    }

    /// <summary>
    /// Unit test to verify that a card with no value draws the card itself, keeps the class the view gave it, and
    /// leaves its attribute out of the page.
    /// </summary>
    [TestMethod]
    public void Process_NoValue_DrawsTheComponentAndKeepsTheViewsClass()
    {
        // Arrange (Given)
        TagHelperAttribute attribute = new TagHelperAttribute("card");
        TagHelperAttribute viewClass = new TagHelperAttribute("class", "mt-4");

        // Act (When)
        TagHelperOutput output = Process(new CardTagHelper(), attribute, viewClass);
        string[] classes = ClassesOf(output);
        bool card = classes.Contains("shadow-card");
        bool kept = classes.Contains("mt-4");
        bool header = classes.Contains("border-b");
        bool attributeLeft = output.Attributes.ContainsName("card");

        // Assert (Then)
        Assert.AreEqual(
            (true, true, false, false),
            (card, kept, header, attributeLeft),
            "The card should have its own utilities and the view's class, and lose its attribute.");
    }

    /// <summary>
    /// Unit test to verify that an attribute with an empty value, as a view writes for a version that is not the
    /// current one, draws the component itself.
    /// </summary>
    [TestMethod]
    public void Process_EmptyValue_DrawsTheComponent()
    {
        // Arrange (Given)
        TagHelperAttribute attribute = new TagHelperAttribute("version", string.Empty);

        // Act (When)
        TagHelperOutput output = Process(new VersionTagHelper(), attribute);
        string[] classes = ClassesOf(output);
        bool version = classes.Contains("bg-brand-subtle");
        bool current = classes.Contains("bg-brand");

        // Assert (Then)
        Assert.AreEqual(
            (true, false),
            (version, current),
            "An empty value should draw a version badge that is not the current one.");
    }

    /// <summary>
    /// Unit test to verify that an element whose attribute Razor left out, as it does when the view's value is null,
    /// gets no utilities.
    /// </summary>
    [TestMethod]
    public void Process_AttributeLeftOut_AddsNothing()
    {
        // Arrange (Given)
        TagHelperContext context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            "grid");
        TagHelperOutput output = CreateOutput(new TagHelperAttributeList());

        // Act (When)
        new GridTagHelper().Process(context, output);
        int classes = ClassesOf(output).Length;

        // Assert (Then)
        Assert.AreEqual(
            0,
            classes,
            "A row whose grid attribute was left out should get no utilities.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Makes the tag helper that draws the component an attribute names.
    /// </summary>
    /// <param name="name">The component's attribute.</param>
    /// <returns>The tag helper.</returns>
    private static ComponentTagHelper CreateHelper(string name)
    {
        return name switch
        {
            "alert" =>
                new AlertTagHelper(),
            "breadcrumbs" =>
                new BreadcrumbsTagHelper(),
            "busy-bar" =>
                new BusyBarTagHelper(),
            "card" =>
                new CardTagHelper(),
            "dialog" =>
                new DialogTagHelper(),
            "empty-state" =>
                new EmptyStateTagHelper(),
            "field" =>
                new FieldTagHelper(),
            "fields" =>
                new FieldsTagHelper(),
            "field-rows" =>
                new FieldRowsTagHelper(),
            "filter-box" =>
                new FilterBoxTagHelper(),
            "form-actions" =>
                new FormActionsTagHelper(),
            "grid" =>
                new GridTagHelper(),
            "hero" =>
                new HeroTagHelper(),
            "menu" =>
                new MenuTagHelper(),
            "page-header" =>
                new PageHeaderTagHelper(),
            "pager" =>
                new PagerTagHelper(),
            "related" =>
                new RelatedTagHelper(),
            "relation" =>
                new RelationTagHelper(),
            "segmented" =>
                new SegmentedTagHelper(),
            "tabs" =>
                new TabsTagHelper(),
            "toolbar" =>
                new ToolbarTagHelper(),
            "version" =>
                new VersionTagHelper(),
            _ =>
                throw new ArgumentOutOfRangeException(nameof(name), name, "No component has that attribute."),
        };
    }

    /// <summary>
    /// Runs a tag helper on an element, as Razor would.
    /// </summary>
    /// <param name="helper">The tag helper.</param>
    /// <param name="attribute">The component's attribute, as the view wrote it.</param>
    /// <param name="others">The element's other attributes.</param>
    /// <returns>The element as it will be written.</returns>
    private static TagHelperOutput Process(
        ComponentTagHelper helper,
        TagHelperAttribute attribute,
        params TagHelperAttribute[] others)
    {
        TagHelperAttributeList attributes = new TagHelperAttributeList(others) { attribute };
        TagHelperContext context = new TagHelperContext(
            attributes,
            new Dictionary<object, object>(),
            attribute.Name);
        TagHelperOutput output = CreateOutput(new TagHelperAttributeList(attributes));
        helper.Process(context, output);
        return output;
    }

    /// <summary>
    /// Makes an element as Razor hands it to a tag helper.
    /// </summary>
    /// <param name="attributes">The attributes the view wrote.</param>
    /// <returns>The element.</returns>
    private static TagHelperOutput CreateOutput(TagHelperAttributeList attributes)
    {
        return new TagHelperOutput(
            "div",
            attributes,
            static (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    /// <summary>
    /// Reads the classes an element was given, as the browser reads them: the tag helper writes each one HTML-encoded,
    /// so a utility such as <c>[&amp;>svg]:size-10</c> arrives with its ampersand encoded.
    /// </summary>
    /// <param name="output">The element.</param>
    /// <returns>Its classes, one to an entry.</returns>
    private static string[] ClassesOf(TagHelperOutput output)
    {
        TagHelperAttributeList attributes = output.Attributes;
        string value = WebUtility.HtmlDecode(attributes["class"]?.Value?.ToString()) ?? string.Empty;
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    #endregion Private Methods
}
