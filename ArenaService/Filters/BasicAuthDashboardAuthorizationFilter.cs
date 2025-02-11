namespace ArenaService.Filter;

using System;
using System.Text;
using ArenaService.Options;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

public class BasicAuthDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;

    public BasicAuthDashboardAuthorizationFilter(IOptions<OpsConfigOptions> config)
    {
        _username = config.Value.HangfireUsername;
        _password = config.Value.HangfirePassword;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var authHeader = httpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
        {
            return false;
        }

        var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
        var credentials = Encoding
            .UTF8.GetString(Convert.FromBase64String(encodedCredentials))
            .Split(':');

        return credentials.Length == 2
            && credentials[0] == _username
            && credentials[1] == _password;
    }
}
