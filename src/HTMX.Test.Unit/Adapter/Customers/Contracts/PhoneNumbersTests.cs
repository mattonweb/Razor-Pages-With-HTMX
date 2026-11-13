namespace HTMX.Test.Unit.Adapter.Customers.Contracts;

using System;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="PhoneNumbers"/> class: a contact's telephone
/// numbers, which are equal when they hold the same numbers in the same order.
/// </summary>
[TestClass]
public sealed class PhoneNumbersTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that two lists of the same numbers in the same order are equal, with the same hash, though
    /// they are different arrays.
    /// </summary>
    [TestMethod]
    public void Equals_SameNumbersInTheSameOrder_IsEqual()
    {
        // Arrange (Given)
        PhoneNumbers first = new PhoneNumbers([new PhoneNumber("+1 555-0142"), new PhoneNumber("+1 555-0199")]);
        PhoneNumbers second = new PhoneNumbers([new PhoneNumber("+1 555-0142"), new PhoneNumber("+1 555-0199")]);

        // Act (When)
        bool equal = first == second;

        // Assert (Then)
        Assert.AreEqual(
            (true, first.GetHashCode()),
            (equal, second.GetHashCode()),
            "Lists of the same numbers in the same order should be equal, with the same hash.");
    }

    /// <summary>
    /// Unit test to verify that the same numbers in another order, or fewer numbers, are not equal: a save that
    /// reorders or removes numbers changes the contact.
    /// </summary>
    /// <param name="second">The other list's numbers, separated by commas.</param>
    [TestMethod]
    [DataRow("+1 555-0199,+1 555-0142", DisplayName = "Another order")]
    [DataRow("+1 555-0142", DisplayName = "Fewer numbers")]
    public void Equals_OtherOrderOrNumbers_IsNotEqual(string second)
    {
        // Arrange (Given)
        PhoneNumbers first = new PhoneNumbers([new PhoneNumber("+1 555-0142"), new PhoneNumber("+1 555-0199")]);
        PhoneNumber[] numbers = Array.ConvertAll(second.Split(','), static value => new PhoneNumber(value));
        PhoneNumbers other = new PhoneNumbers(numbers);

        // Act (When)
        bool equal = first == other;

        // Assert (Then)
        Assert.IsFalse(
            equal,
            "Lists that differ in their numbers or their order should not be equal.");
    }

    /// <summary>
    /// Unit test to verify that the list keeps a copy of the numbers it is given, so changing that array later
    /// changes nothing.
    /// </summary>
    [TestMethod]
    public void Constructor_ArrayChangedLater_KeepsTheNumbersAsGiven()
    {
        // Arrange (Given)
        PhoneNumber[] given = [new PhoneNumber("+1 555-0142")];
        PhoneNumbers numbers = new PhoneNumbers(given);

        // Act (When)
        given[0] = new PhoneNumber("+1 555-0100");

        // Assert (Then)
        Assert.AreEqual(
            new PhoneNumber("+1 555-0142"),
            numbers.Values[0],
            "The list should keep the number it was given.");
    }

    #endregion Public Methods
}
