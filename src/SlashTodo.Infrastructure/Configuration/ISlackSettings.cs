using System;

namespace SlashTodo.Infrastructure.Configuration
{
    public interface ISlackSettings
    {
        string ClientId { get; }
        string ClientSecret { get; }
        Uri OAuthAuthorizationUrl { get; }
        Uri OAuthRedirectUrl { get; }
        string OAuthScope { get; }
        string ApiBaseUrl { get; }
    }
}