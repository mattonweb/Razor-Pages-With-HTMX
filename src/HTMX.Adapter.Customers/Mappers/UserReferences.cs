namespace HTMX.Adapter.Customers.Mappers;

using System;
using System.Linq.Expressions;
using HTMX.Adapter.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Makes the columns that name a user, such as CreatedBy and ChangedBy, foreign keys to the Users table. Every entity
/// records who created it, changed it and archived it, so every mapper uses this.
/// </summary>
internal static class UserReferences
{
    /// <summary>
    /// Makes a column a foreign key to a user.
    /// </summary>
    /// <typeparam name="TEntity">The entity whose column names the user.</typeparam>
    /// <param name="builder">The builder for that entity.</param>
    /// <param name="foreignKey">The column that holds the user's identifier.</param>
    /// <param name="onDelete">What deleting that user does to this row; usually
    /// <see cref="DeleteBehavior.Restrict"/>.</param>
    internal static void Add<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object?>> foreignKey,
        DeleteBehavior onDelete)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ReferenceCollectionBuilder<User, TEntity> reference = builder.HasOne<User>().WithMany();
        reference.HasForeignKey(foreignKey);
        reference.OnDelete(onDelete);
    }
}
