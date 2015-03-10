using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stateless;

namespace SlashTodo.Core.Domain
{
    public class User : Aggregate
    {
        private UserState _state = UserState.Initial;
        private StateMachine<UserState, UserTrigger> _stateMachine;
        private StateMachine<UserState, UserTrigger>.TriggerWithParameters<string, string> _createTrigger;
        private string _teamId;
        private string _name;
        private string _slackApiAccessToken;

        public string TeamId { get { return _teamId; } }
        public string Name { get { return _name; } }
        public string SlackApiAccessToken { get { return _slackApiAccessToken; } }

        public User()
        {
            ConfigureStateMachine();
        }

        public static User Create(string id, string teamId)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }
            if (string.IsNullOrWhiteSpace(teamId))
            {
                throw new ArgumentNullException("teamId");
            }
            var user = new User();
            if (!user._stateMachine.CanFire(UserTrigger.Create))
            {
                throw new InvalidOperationException();
            }
            user.Id = id;
            user.RaiseEvent(new UserCreated
            {
                TeamId = teamId
            });
            if (user._stateMachine.CanFire(UserTrigger.Activate))
            {
                user.RaiseEvent(new UserActivated());
            }
            return user;
        }

        public void UpdateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }
            name = name.Trim();
            if (string.IsNullOrEmpty(_name) ||
                !name.Equals(_name, StringComparison.Ordinal))
            {
                RaiseEvent(new UserNameUpdated { Name = name });
            }
        }

        public void UpdateSlackApiAccessToken(string slackApiAccessToken)
        {
            if (string.Equals(_slackApiAccessToken, slackApiAccessToken, StringComparison.Ordinal))
            {
                return;
            }
            if (slackApiAccessToken != null &&
                string.IsNullOrWhiteSpace(slackApiAccessToken))
            {
                // A token cannot be empty or only consist of whitespace
                throw new ArgumentOutOfRangeException("slackApiAccessToken", "Access token may not only consist of whitespace.");
            }
            RaiseEvent(new UserSlackApiAccessTokenUpdated { SlackApiAccessToken = slackApiAccessToken });
        }

        protected override void ApplyEventCore(IDomainEvent @event)
        {
            Apply((dynamic)@event);
        }

        private void Apply(UserCreated @event)
        {
            _stateMachine.Fire(_createTrigger, @event.Id, @event.TeamId);
        }

        private void Apply(UserActivated @event)
        {
            _stateMachine.Fire(UserTrigger.Activate);
        }

        private void Apply(UserNameUpdated @event)
        {
            _name = @event.Name;
        }

        private void Apply(UserSlackApiAccessTokenUpdated @event)
        {
            _slackApiAccessToken = @event.SlackApiAccessToken;
        }

        private void ConfigureStateMachine()
        {
            _stateMachine = new StateMachine<UserState, UserTrigger>(
                () => _state,
                state => _state = state
                );
            _createTrigger = _stateMachine.SetTriggerParameters<string, string>(UserTrigger.Create);

            _stateMachine.Configure(UserState.Initial)
                .Permit(UserTrigger.Create, UserState.Created);
            _stateMachine.Configure(UserState.Created)
                .OnEntryFrom(_createTrigger, (id, teamId) =>
                {
                    Id = id;
                    _teamId = teamId;
                })
                .Permit(UserTrigger.Activate, UserState.Active);
        }

        public enum UserState
        {
            Initial,
            Created,
            Active
        }

        public enum UserTrigger
        {
            Create,
            Activate
        }
    }
}
