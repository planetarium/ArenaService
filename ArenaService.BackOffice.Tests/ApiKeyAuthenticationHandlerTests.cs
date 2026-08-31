using System.Text.Encodings.Web;
using ArenaService.BackOffice.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ArenaService.BackOffice.Tests;

public class ApiKeyAuthenticationHandlerTests
{
    private const string ConfiguredKey = "configured-secret";

    private static async Task<AuthenticateResult> AuthenticateAsync(
        string? configuredKey,
        string? providedKey,
        bool sendHeader = true
    )
    {
        var settings = new Dictionary<string, string?>();
        if (configuredKey is not null)
        {
            settings[ApiKeyAuthenticationDefaults.ConfigurationKey] = configuredKey;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var optionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor
            .Setup(o => o.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());

        var handler = new ApiKeyAuthenticationHandler(
            optionsMonitor.Object,
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            configuration
        );

        var context = new DefaultHttpContext();
        if (sendHeader)
        {
            context.Request.Headers[ApiKeyAuthenticationDefaults.HeaderName] = providedKey;
        }

        await handler.InitializeAsync(
            new AuthenticationScheme(
                ApiKeyAuthenticationDefaults.AuthenticationScheme,
                null,
                typeof(ApiKeyAuthenticationHandler)
            ),
            context
        );

        return await handler.AuthenticateAsync();
    }

    [Fact]
    public async Task WhenApiKeyMatches_ShouldSucceed()
    {
        var result = await AuthenticateAsync(ConfiguredKey, ConfiguredKey);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ApiKeyAuthenticationDefaults.AuthenticationScheme,
            result.Ticket!.AuthenticationScheme
        );
        Assert.Equal("ApiUser", result.Principal!.Identity!.Name);
    }

    [Fact]
    public async Task WhenHeaderIsMissing_ShouldReturnNoResult()
    {
        var result = await AuthenticateAsync(ConfiguredKey, null, sendHeader: false);

        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WhenHeaderIsBlank_ShouldFail(string providedKey)
    {
        var result = await AuthenticateAsync(ConfiguredKey, providedKey);

        Assert.False(result.Succeeded);
        Assert.False(result.None);
    }

    [Fact]
    public async Task WhenApiKeyDoesNotMatch_ShouldFail()
    {
        var result = await AuthenticateAsync(ConfiguredKey, "wrong-secret");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid API Key.", result.Failure!.Message);
    }

    [Fact]
    public async Task WhenApiKeyDiffersOnlyByCase_ShouldFail()
    {
        var result = await AuthenticateAsync(ConfiguredKey, ConfiguredKey.ToUpperInvariant());

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task WhenApiKeyIsNotConfigured_ShouldFail()
    {
        var result = await AuthenticateAsync(null, ConfiguredKey);

        Assert.False(result.Succeeded);
        Assert.Equal("API Key authentication is not configured.", result.Failure!.Message);
    }
}
