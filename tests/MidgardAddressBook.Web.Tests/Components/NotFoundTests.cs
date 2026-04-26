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

        cut.Find("h1").TextContent.Should().Be("Page not found");
        cut.Markup.Should().Contain("The page you're looking for doesn't exist or has been moved.");
    }
}
