namespace ArenaService.Tests.Auth;

using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using global::ArenaService.Auth;
using Libplanet.Common;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Xunit;

public class ES256KAuthenticationHandlerTests
{
    private readonly Mock<IOptionsMonitor<AuthenticationSchemeOptions>> _optionsMonitor;
    private readonly Mock<ILoggerFactory> _loggerFactory;
    private readonly Mock<UrlEncoder> _encoder;
    private readonly Mock<ISystemClock> _clock;

    public ES256KAuthenticationHandlerTests()
    {
        _optionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        _optionsMonitor
            .Setup(m => m.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());
        _optionsMonitor.Setup(m => m.CurrentValue).Returns(new AuthenticationSchemeOptions());
        _loggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger<ES256KAuthenticationHandler>>();
        _loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        _encoder = new Mock<UrlEncoder>();
        _clock = new Mock<ISystemClock>();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ValidUserRole_ReturnsSuccess()
    {
        var privateKey = new PrivateKey();
        var jwt = JwtCreator.CreateJwt(privateKey, role: "User");
        var publicKey = privateKey.PublicKey;
        var address = publicKey.Address.ToString();

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = $"Bearer {jwt}";

        var handler = new ES256KAuthenticationHandler(
            _optionsMonitor.Object,
            _loggerFactory.Object,
            _encoder.Object,
            _clock.Object
        );

        await handler.InitializeAsync(
            new AuthenticationScheme("ES256K", null, typeof(ES256KAuthenticationHandler)),
            context
        );

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal(publicKey.ToString(), result.Principal.FindFirst("public_key")?.Value);
        Assert.Equal(address, result.Principal.FindFirst("address")?.Value);
        Assert.IsType<string>(result.Principal.FindFirst("avatar_address")?.Value);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ValidAdminRole_ReturnsSuccess()
    {
        var privateKey = new PrivateKey();
        var jwt = JwtCreator.CreateJwt(privateKey, role: "Admin");
        var publicKey = privateKey.PublicKey;

        Environment.SetEnvironmentVariable("ALLOWED_ADMIN_PUBLIC_KEY", publicKey.ToHex(true));

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = $"Bearer {jwt}";

        var handler = new ES256KAuthenticationHandler(
            _optionsMonitor.Object,
            _loggerFactory.Object,
            _encoder.Object,
            _clock.Object
        );

        await handler.InitializeAsync(
            new AuthenticationScheme("ES256K", null, typeof(ES256KAuthenticationHandler)),
            context
        );

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal(publicKey.ToString(), result.Principal.FindFirst("public_key")?.Value);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_InvalidAdminKey_ReturnsFail()
    {
        var privateKey = new PrivateKey();
        var jwt = JwtCreator.CreateJwt(privateKey, role: "Admin");

        Environment.SetEnvironmentVariable(
            "ALLOWED_ADMIN_PUBLIC_KEY",
            new PrivateKey().PublicKey.ToHex(true)
        );

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = $"Bearer {jwt}";

        var handler = new ES256KAuthenticationHandler(
            _optionsMonitor.Object,
            _loggerFactory.Object,
            _encoder.Object,
            _clock.Object
        );

        await handler.InitializeAsync(
            new AuthenticationScheme("ES256K", null, typeof(ES256KAuthenticationHandler)),
            context
        );

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_MissingToken_ReturnsFail()
    {
        var context = new DefaultHttpContext();
        var handler = new ES256KAuthenticationHandler(
            _optionsMonitor.Object,
            _loggerFactory.Object,
            _encoder.Object,
            _clock.Object
        );

        await handler.InitializeAsync(
            new AuthenticationScheme("ES256K", null, typeof(ES256KAuthenticationHandler)),
            context
        );

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Principal);
        Assert.Contains("Missing or invalid Authorization header.", result.Failure.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_InvalidTokenFormat_ReturnsFail()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer invalid.token.format";

        var handler = new ES256KAuthenticationHandler(
            _optionsMonitor.Object,
            _loggerFactory.Object,
            _encoder.Object,
            _clock.Object
        );

        await handler.InitializeAsync(
            new AuthenticationScheme("ES256K", null, typeof(ES256KAuthenticationHandler)),
            context
        );

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Principal);
        Assert.Contains("Invalid token.", result.Failure.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ExpiredToken_ReturnsFail()
    {
        var privateKey = new PrivateKey();
        var payload = new
        {
            iss = "user",
            avt_adr = "test",
            pbk = privateKey.PublicKey.ToHex(true),
            role = "User",
            iat = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds(),
            exp = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds()
        };

        string payloadJson = JsonConvert.SerializeObject(payload);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        byte[] hash = HashDigest<SHA256>.DeriveFrom(payloadBytes).ToByteArray();
        byte[] signature = privateKey.Sign(hash);

        string signatureBase64 = Convert.ToBase64String(signature);
        string headerJson = JsonConvert.SerializeObject(new { alg = "ES256K", typ = "JWT" });
        string headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson));
        string payloadBase64 = Convert.ToBase64String(payloadBytes);

        var jwt = $"{headerBase64}.{payloadBase64}.{signatureBase64}";

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = $"Bearer {jwt}";

        var handler = new ES256KAuthenticationHandler(
            _optionsMonitor.Object,
            _loggerFactory.Object,
            _encoder.Object,
            _clock.Object
        );

        await handler.InitializeAsync(
            new AuthenticationScheme("ES256K", null, typeof(ES256KAuthenticationHandler)),
            context
        );

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Principal);
        Assert.Contains("Invalid token.", result.Failure.Message);
    }
}
