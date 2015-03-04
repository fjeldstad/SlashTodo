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
        private StateMachine<UserState, UserTrigger>.TriggerWithParameters<Guid, Guid, string> _createTrigger;
        private Guid _accountId;
        private string _slackUserId;
        private string _slackUserName;
        private string _slackApiAccessToken;

        public Guid AccountId { get { return _accountId; } }
        public string SlackUserId { get { return _slackUserId; } }
        public string SlackUserName { get { return _slackUserName; } }
        public string SlackApiAccessToken { get { return _slackApiAccessToken; } }

        public User()
        {
            ConfigureStateMachine();
        }

        public static User Create(Guid id, Guid accountId, string slackUserId)
        {
            if (string.IsNullOrWhiteSpace(slackUserId))
            {
                throw new ArgumentNullException("slackUserId");
            }
            var user = new User();
            if (!user._stateMachine.CanFire(UserTrigger.Create))
            {
                throw new InvalidOperationException();
            }
            user.Id = id;
            user.RaiseEvent(new UserCreated
            {
                AccountId = accountId,
                SlackUserId = slackUserId
            });
            user.RaiseEvent(new UserActivated());
            return user;
        }

        public void UpdateSlackUserName(string slackUserName)
        {
            if (string.IsNullOrWhiteSpace(slackUserName))
            {
                throw new ArgumentNullException("slackUserName");
            }
            slackUserName = slackUserName.Trim();
            if (string.IsNullOrEmpty(_slackUserName) ||
                !slackUserName.Equals(_slackUserName, StringComparison.Ordinal))
            {
                RaiseEvent(new UserSlackUserNameUpdated { SlackUserName = slackUserName });
            }
        }

        public void UpdateSlackApiAccessToken(string slackApiAccessToken)
        {
            if (slackApiAccessToken == null)
            {
                if (_slackApiAccessToken != null)
                {
                    RaiseEvent(new UserSlackApiAccessTokenRemoved());
                }
                return;
            }
            if (string.IsNullOrWhiteSpace(slackApiAccessToken))
            {
                throw new ArgumentNullException("slackApiAccessToken");
            }
            if (_slackApiAccessToken == null ||
                !slackApiAccessToken.Equals(_slackApiAccessToken, StringComparison.Ordinal))
            {
                RaiseEvent(new UserSlackApiAccessTokenUpdated { SlackApiAccessToken = slackApiAccessToken });
            }
        }

        protected override void ApplyEventCore(IDomainEvent @event)
        {
            Apply((dynamic)@event);
        }

        private void Apply(UserCreated @event)
        {
            _stateMachine.Fire(_createTrigger, @event.Id, @event.AccountId, @event.SlackUserId);
        }

        private void Apply(UserActivated @event)
        {
            _stateMachine.Fire(UserTrigger.Activate);
        }

        private void Apply(UserSlackUserNameUpdated @event)
        {
            _slackUserName = @event.SlackUserName;
        }

        private void Apply(UserSlackApiAccessTokenUpdated @event)
        {
            _slackApiAccessToken = @event.SlackApiAccessToken;
        }

        private void Apply(UserSlackApiAccessTokenRemoved @event)
        {
            _slackApiAccessToken = null;
        }

        private void ConfigureStateMachine()
        {
            _stateMachine = new StateMachine<UserState, UserTrigger>(
                () => _state,
                state => _state = state
                );
            _createTrigger = _stateMachine.SetTriggerParameters<Guid, Guid, string>(UserTrigger.Create);

            _stateMachine.Configure(UserState.Initial)
                .Permit(UserTrigger.Create, UserState.Created);
            _stateMachine.Configure(UserState.Created)
                .OnEntryFrom(_createTrigger, (id, accountId, slackUserId) =>
                {
                    Id = id;
                    _accountId = accountId;
                    _slackUserId = slackUserId;
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
