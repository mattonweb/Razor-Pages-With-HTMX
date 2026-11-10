namespace HTMX.Adapter.Customers.Data;

using System;
using System.Linq.Expressions;
using System.Reflection;
using HTMX.Adapter.Customers.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

/// <summary>
/// Teaches Entity Framework the comparison methods of the adapter's extension classes, such as
/// <see cref="ContactIdExtensions.IsGreaterThan"/>, so that a query that calls one becomes a plain SQL comparison:
/// <c>"c"."Id" &gt; @id</c>.
/// </summary>
/// <remarks>
/// A wrapped value such as <see cref="Contracts.ContactId"/> has no comparison operators of its own: how values order
/// is the database's business, not the contract's. Key-based paging still needs "the rows past this one", and Entity
/// Framework cannot run a C# method inside SQL, so each method is mapped as a database function whose translation is
/// the comparison itself. The comparison names the bare column, so the database can seek in an index that starts with
/// it; a cast to the stored type would become a SQL CAST, and the database would read the index from its start. Every
/// extension class that orders a value names its four comparisons alike, and a class that lacks one fails here, as
/// the model is built, rather than in a query.
/// </remarks>
internal static class ComparisonTranslations
{
    /// <summary>
    /// Maps an extension class's four comparison methods: IsLessThan, IsGreaterThan, IsLessThanOrEqualTo and
    /// IsGreaterThanOrEqualTo.
    /// </summary>
    /// <param name="modelBuilder">The builder for the model.</param>
    /// <param name="extensions">The extension class, such as <see cref="ContactIdExtensions"/>.</param>
    internal static void Map(ModelBuilder modelBuilder, Type extensions)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(extensions);
        MapOne(modelBuilder, extensions, nameof(ContactIdExtensions.IsLessThan), ExpressionType.LessThan);
        MapOne(modelBuilder, extensions, nameof(ContactIdExtensions.IsGreaterThan), ExpressionType.GreaterThan);
        MapOne(
            modelBuilder,
            extensions,
            nameof(ContactIdExtensions.IsLessThanOrEqualTo),
            ExpressionType.LessThanOrEqual);
        MapOne(
            modelBuilder,
            extensions,
            nameof(ContactIdExtensions.IsGreaterThanOrEqualTo),
            ExpressionType.GreaterThanOrEqual);
    }

    /// <summary>
    /// Maps one comparison method as a function whose SQL is the comparison of its two arguments.
    /// </summary>
    /// <param name="modelBuilder">The builder for the model.</param>
    /// <param name="extensions">The extension class.</param>
    /// <param name="name">The method's name.</param>
    /// <param name="comparison">The comparison it stands for.</param>
    private static void MapOne(ModelBuilder modelBuilder, Type extensions, string name, ExpressionType comparison)
    {
        MethodInfo method = extensions.GetMethod(name, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{extensions.Name} has no {name} to map.");
        modelBuilder
            .HasDbFunction(method)
            .HasTranslation(arguments =>
                new SqlBinaryExpression(comparison, arguments[0], arguments[1], typeof(bool), typeMapping: null));
    }
}
