using Bunit;
using FluentAssertions;
using MidgardAddressBook.Web.Components.Pages;
using Xunit;

namespace MidgardAddressBook.Web.Tests.Components.Pages;

/// <summary>
/// bUnit tests for the <see cref="Splash"/> Blazor page covering structure,
/// navigation, accessibility, and content rendering.
/// </summary>
public class SplashTests : BunitContext
{
    // ── Structure / layout ────────────────────────────────────────────────────

    [Fact]
    public void Should_RenderSplashHeroSection_When_PageLoads()
    {
        // Arrange & Act
        var cut = Render<Splash>();

        // Assert
        cut.Find(".splash-hero").Should().NotBeNull();
    }

    // ── Heading ───────────────────────────────────────────────────────────────

    [Fact]
    public void Should_RenderH1Heading_When_PageLoads()
    {
        // Arrange & Act
        var cut = Render<Splash>();

        // Assert
        cut.Find("h1").TextContent.Should().Contain("Midgard Address Book");
    }

    // ── Navigation link ───────────────────────────────────────────────────────

    [Fact]
    public void Should_RenderStartAddressBookLink_WithContactsHref_When_PageLoads()
    {
        // Arrange & Act
        var cut = Render<Splash>();

        // Assert
        var link = cut.Find("a.splash-cta");
        link.GetAttribute("href").Should().Be("/contacts");
        link.TextContent.Trim().Should().Contain("Start Address Book");
    }

    // ── Accessibility ─────────────────────────────────────────────────────────

    [Fact]
    public void Should_HaveAriaHiddenOnSplashIconContainer_When_PageLoads()
    {
        // Arrange & Act
        var cut = Render<Splash>();

        // Assert
        cut.Find(".splash-icon").GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void Should_HaveAriaHiddenOnCtaIcon_When_PageLoads()
    {
        // Arrange & Act
        var cut = Render<Splash>();

        // Assert
        cut.Find("a.splash-cta i").GetAttribute("aria-hidden").Should().Be("true");
    }

    // ── Tagline paragraph ─────────────────────────────────────────────────────

    [Fact]
    public void Should_RenderTaglineParagraph_When_PageLoads()
    {
        // Arrange & Act
        var cut = Render<Splash>();

        // Assert
        cut.Find(".splash-hero p").Should().NotBeNull();
    }

    [Fact]
    public void Should_RenderTaglineParagraphWithNorseContent_When_PageLoads()
    {
        // Arrange & Act
        var cut = Render<Splash>();

        // Assert
        cut.Find(".splash-tagline").TextContent.Should().NotBeNullOrWhiteSpace();
        cut.Find(".splash-tagline").TextContent.Should().Contain("Nine Realms");
    }

    // ── Font Awesome icon presence ────────────────────────────────────────────

    [Fact]
    public void Should_RenderAddressBookIcon_When_PageLoads()
    {
        // Arrange & Act
        var cut = Render<Splash>();

        // Assert
        cut.FindAll("i.fa-address-book").Should().NotBeEmpty();
    }
}
