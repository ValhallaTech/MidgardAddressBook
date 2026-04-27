using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using MidgardAddressBook.Web.Components.Layout;
using Xunit;

namespace MidgardAddressBook.Web.Tests.Components.Layout;

/// <summary>
/// Smoke tests for <see cref="MainLayout"/> verifying it renders its body content and the
/// blazor error UI host element used by the Blazor runtime.
/// </summary>
public class MainLayoutTests : BunitContext
{
    [Fact]
    public void Renders_BodyContent_AndErrorUiHost()
    {
        var cut = Render<MainLayout>(parameters =>
            parameters.Add<RenderFragment>(
                p => p.Body!,
                builder => builder.AddMarkupContent(0, "<p id=\"hello\">hi</p>")
            )
        );

        cut.Find("p#hello").TextContent.Should().Be("hi");
        cut.Find("div#blazor-error-ui").Should().NotBeNull();
        cut.Markup.Should().Contain("An unhandled error has occurred.");
    }
}
