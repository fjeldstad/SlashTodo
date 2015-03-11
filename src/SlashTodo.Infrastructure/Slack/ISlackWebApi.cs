using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Refit;

namespace SlashTodo.Infrastructure.Slack
{
    public interface ISlackWebApi
    {
        [Post("/oauth.access")]
        Task<OAuthAccessResponse> OAuthAccess([Body(BodySerializationMethod.UrlEncoded)] OAuthAccessRequest request);

        [Post("/auth.test")]
        Task<AuthTestResponse> AuthTest([Body(BodySerializationMethod.UrlEncoded)] AuthTestRequest request);

        [Post("/users.info")]
        Task<UsersInfoResponse> UsersInfo([Body(BodySerializationMethod.UrlEncoded)] UsersInfoRequest request);
    }

    public abstract class AuthenticatedRequestBase
    {
        [AliasAs("token")]
        public string AccessToken { get; set; }
    }

    public abstract class ResponseBase
    {
        [JsonProperty(PropertyName = "ok")]
        public bool Ok { get; set; }

        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }
    }

    public class OAuthAccessRequest
    {
        [AliasAs("client_id")]
        public string ClientId { get; set; }

        [AliasAs("client_secret")]
        public string ClientSecret { get; set; }

        [AliasAs("code")]
        public string Code { get; set; }

        [AliasAs("redirect_uri")]
        public string RedirectUri { get; set; }
    }

    public class OAuthAccessResponse : ResponseBase
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "scope")]
        public string Scope { get; set; }
    }

    public class AuthTestRequest : AuthenticatedRequestBase
    {
    }

    public class AuthTestResponse : ResponseBase
    {
        [JsonProperty(PropertyName = "team")]
        public string TeamName { get; set; }

        [JsonProperty(PropertyName = "user")]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "team_id")]
        public string TeamId { get; set; }

        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string TeamUrl { get; set; }
    }

    public class UsersInfoRequest : AuthenticatedRequestBase
    {
        [AliasAs("user")]
        public string UserId { get; set; }
    }

    public class UsersInfoResponse : ResponseBase
    {
        [JsonProperty(PropertyName = "user")]
        public User User { get; set; }
    }

    public class User
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "is_admin")]
        public bool IsAdmin { get; set; }
    }
}
