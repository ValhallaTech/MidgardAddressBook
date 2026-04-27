using System.Diagnostics;
using Bunit;
using FluentAssertions;
using MidgardAddressBook.Web.Components.Pages;
using Xunit;

namespace MidgardAddressBook.Web.Tests.Components.Pages;

/// <summary>
/// bUnit tests for <see cref="Error"/> covering both the "with request id" and "without
/// request id" rendering branches.
/// </summary>
public class ErrorTests : BunitContext
{
    [Fact]
    public void RendersHeading_AndNoRequestId_WhenActivityAndHttpContextAreAbsent()
    {
        // Make sure no ambient Activity leaks in from another test.
        Activity.Current = null;

        var cut = Render<Error>();

        cut.Find("h1").TextContent.Should().Be("An error occurred");
        cut.Markup.Should().NotContain("Request ID:");
    }

    [Fact]
    public void RendersRequestId_FromCurrentActivity()
    {
        using var activity = new Activity("test-activity");
        activity.Start();

        try
        {
            var cut = Render<Error>();

            cut.Markup.Should().Contain("Request ID:");
            cut.Markup.Should().Contain(activity.Id!);
        }
        finally
        {
            activity.Stop();
            Activity.Current = null;
        }
    }
}
