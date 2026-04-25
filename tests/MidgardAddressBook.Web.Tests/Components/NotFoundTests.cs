using Bunit;
using FluentAssertions;
using MidgardAddressBook.Web.Components.Pages;
using Xunit;

namespace MidgardAddressBook.Web.Tests.Components;

/// <summary>
/// Smoke tests for the <see cref="NotFound"/> Blazor page using bUnit.
/// </summary>
public class NotFoundTests : TestContext
{
    [Fact]
    public void RendersHeadingAndApologyText()
    {
        var cut = RenderComponent<NotFound>();

        cut.Find("h3").TextContent.Should().Be("Not Found");
        cut.Markup.Should().Contain("Sorry, the content you are looking for does not exist.");
    }
}
