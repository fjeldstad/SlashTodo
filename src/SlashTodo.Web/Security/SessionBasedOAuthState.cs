using System;
using Nancy.Session;
using SlashTodo.Infrastructure;

namespace SlashTodo.Web.Security
{
    public class SessionBasedOAuthState : IOAuthState
    {
        public const string OAuthStateSessionKey = "OAuthState";
        private readonly ISession _session;

        public SessionBasedOAuthState(ISession session)
        {
            _session = session;
        }

        public string Generate()
        {
            var state = Guid.NewGuid().ToString();
            _session[OAuthStateSessionKey] = state;
            return state;
        }

        public bool Validate(string state)
        {
            var storedState = _session[OAuthStateSessionKey] as string;
            return !string.IsNullOrWhiteSpace(state) &&
                   !string.IsNullOrWhiteSpace(storedState) &&
                   state.Equals(storedState, StringComparison.Ordinal);
        }
    }
}