namespace HTMX.Test.Unit.Application.Web.Data;

using System;
using System.Linq;
using HTMX.Application.Web.Data;
using HTMX.Application.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="InMemoryDatabase"/> class.
/// </summary>
/// <remarks>
/// Each test opens its own database under a name no other test uses, so the tests can run side by side.
/// </remarks>
[TestClass]
public sealed class InMemoryDatabaseTests
{
    private const string CreateNotesTable = "CREATE TABLE Notes (Text TEXT NOT NULL)";

    #region Public Methods

    /// <summary>
    /// Unit test to verify that a database cannot be opened under the empty name a default struct carries.
    /// </summary>
    [TestMethod]
    public void Constructor_DefaultName_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DatabaseName name = default;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            InMemoryDatabase database = new InMemoryDatabase(name);
            database.Dispose();
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A default name should be refused with ArgumentNullException.");
    }

    /// <summary>
    /// Unit test to verify that configuring a missing options builder is refused rather than failing later inside
    /// Entity Framework.
    /// </summary>
    [TestMethod]
    public void Configure_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DbContextOptionsBuilder? options = null;
        bool exceptionThrown = false;

        // Act (When)
        using (InMemoryDatabase database = new InMemoryDatabase(CreateUniqueName()))
        {
            try
            {
                database.Configure(options!);
            }
            catch (ArgumentNullException)
            {
                exceptionThrown = true;
            }
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Null options should be refused with ArgumentNullException.");
    }

    /// <summary>
    /// Unit test to verify the property the application depends on: a change saved through one context, as the edit
    /// page saves it, is read by a second context, as the list page reads it.
    /// </summary>
    [TestMethod]
    public void Configure_ChangeSavedByOneContext_IsReadByAnother()
    {
        // Arrange (Given)
        string[] notes;

        // Act (When)
        using (InMemoryDatabase database = new InMemoryDatabase(CreateUniqueName()))
        {
            Execute(database, CreateNotesTable);
            Execute(database, "INSERT INTO Notes (Text) VALUES ('Saved by the edit page')");
            notes = ReadNotes(database);
        }

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { "Saved by the edit page" },
            notes,
            "The second context should read the row the first context saved.");
    }

    /// <summary>
    /// Unit test to verify that databases with different names are kept apart, which is what lets each test, and
    /// each application, have a database of its own.
    /// </summary>
    [TestMethod]
    public void Configure_DifferentNames_KeepsDatabasesApart()
    {
        // Arrange (Given)
        bool tableInFirst;
        bool tableInSecond;

        // Act (When)
        using (InMemoryDatabase first = new InMemoryDatabase(CreateUniqueName()))
        {
            using (InMemoryDatabase second = new InMemoryDatabase(CreateUniqueName()))
            {
                Execute(first, CreateNotesTable);
                tableInFirst = HasNotesTable(first);
                tableInSecond = HasNotesTable(second);
            }
        }

        // Assert (Then)
        Assert.IsTrue(
            tableInFirst,
            "The table should exist in the database it was created in.");
        Assert.IsFalse(
            tableInSecond,
            "A table created in one database should not appear in a database with another name.");
    }

    /// <summary>
    /// Unit test to verify that disposing the database deletes it, so the application starts from its seed data every
    /// time it runs.
    /// </summary>
    [TestMethod]
    public void Dispose_NoOtherConnectionOpen_DeletesTheDatabase()
    {
        // Arrange (Given)
        DatabaseName name = CreateUniqueName();
        bool tableFound;
        using (InMemoryDatabase first = new InMemoryDatabase(name))
        {
            Execute(first, CreateNotesTable);

            // Act (When): the using block disposes the first database here.
        }

        using (InMemoryDatabase second = new InMemoryDatabase(name))
        {
            tableFound = HasNotesTable(second);
        }

        // Assert (Then)
        Assert.IsFalse(
            tableFound,
            "A database opened again under the same name after disposal should start empty.");
    }

    /// <summary>
    /// Unit test to verify that a disposed database refuses to configure a context, rather than pointing it at a
    /// database that no longer exists.
    /// </summary>
    [TestMethod]
    public void Configure_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange (Given)
        InMemoryDatabase database = new InMemoryDatabase(CreateUniqueName());
        database.Dispose();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            database.Configure(new DbContextOptionsBuilder());
        }
        catch (ObjectDisposedException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Configure should throw ObjectDisposedException after the database is disposed.");
    }

    /// <summary>
    /// Unit test to verify that disposing twice is harmless, as the container and a using block may both dispose.
    /// </summary>
    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrowException()
    {
        // Arrange (Given)
        InMemoryDatabase database = new InMemoryDatabase(CreateUniqueName());
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            database.Dispose();
            database.Dispose();
        }
        catch (ObjectDisposedException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "A second Dispose should do nothing.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a database name that no other test uses.
    /// </summary>
    /// <returns>A new, unique name.</returns>
    private static DatabaseName CreateUniqueName()
    {
        return new DatabaseName(Guid.NewGuid().ToString("N"));
    }

    /// <summary>
    /// Runs one SQL statement through a new context, as one request would, then discards the context.
    /// </summary>
    /// <param name="database">The database to run it in.</param>
    /// <param name="sql">The statement.</param>
    private static void Execute(InMemoryDatabase database, string sql)
    {
        using (DbContext context = CreateContext(database))
        {
            context.Database.ExecuteSqlRaw(sql);
        }
    }

    /// <summary>
    /// Reads every note through a new context, as one request would, then discards the context.
    /// </summary>
    /// <param name="database">The database to read.</param>
    /// <returns>The text of each note.</returns>
    private static string[] ReadNotes(InMemoryDatabase database)
    {
        using (DbContext context = CreateContext(database))
        {
            DatabaseFacade facade = context.Database;
            return [.. facade.SqlQueryRaw<string>("SELECT Text AS Value FROM Notes")];
        }
    }

    /// <summary>
    /// Asks the database, through a new context, whether its Notes table exists.
    /// </summary>
    /// <param name="database">The database to ask.</param>
    /// <returns><see langword="true"/> when the table exists.</returns>
    private static bool HasNotesTable(InMemoryDatabase database)
    {
        using (DbContext context = CreateContext(database))
        {
            DatabaseFacade facade = context.Database;
            int tables = facade.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = 'Notes'")
                .AsEnumerable()
                .Single();
            return tables > 0;
        }
    }

    /// <summary>
    /// Creates a context pointed at the database.
    /// </summary>
    /// <param name="database">The database the context reads and writes.</param>
    /// <returns>A new context, which the caller disposes.</returns>
    private static DbContext CreateContext(InMemoryDatabase database)
    {
        DbContextOptionsBuilder options = new DbContextOptionsBuilder();
        database.Configure(options);
        return new DbContext(options.Options);
    }

    #endregion Private Methods
}
