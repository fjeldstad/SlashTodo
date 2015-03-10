using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stateless;

namespace SlashTodo.Core.Domain
{
    public class Team : Aggregate
    {
        private TeamState _state = TeamState.Initial;
        private StateMachine<TeamState, TeamTrigger> _stateMachine;
        private StateMachine<TeamState, TeamTrigger>.TriggerWithParameters<string> _createTrigger;
        private string _slashCommandToken;
        private Uri _incomingWebhookUrl;
        private string _name;
        private Uri _slackUrl;

        public string Name { get { return _name; } }
        public Uri SlackUrl { get { return _slackUrl; } }
        public string SlashCommandToken { get { return _slashCommandToken; } }
        public Uri IncomingWebhookUrl { get { return _incomingWebhookUrl; } }
        public bool IsActive { get { return _stateMachine.IsInState(TeamState.Active); } }

        protected bool HasValidConfiguration
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_slashCommandToken) &&
                       _incomingWebhookUrl != null;
            }
        }

        public Team()
        {
            ConfigureStateMachine();
        }

        public static Team Create(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }
            var account = new Team();
            if (!account._stateMachine.CanFire(TeamTrigger.Create))
            {
                throw new InvalidOperationException();
            }
            account.Id = id;
            account.RaiseEvent(new TeamCreated());
            return account;
        }

        public void UpdateSlashCommandToken(string slashCommandToken)
        {
            if (string.IsNullOrWhiteSpace(slashCommandToken))
            {
                slashCommandToken = null;
            }
            if (!string.Equals(slashCommandToken, _slashCommandToken, StringComparison.Ordinal))
            {
                RaiseEvent(new TeamSlashCommandTokenUpdated { SlashCommandToken = slashCommandToken });
                if (_stateMachine.CanFire(TeamTrigger.Activate))
                {
                    RaiseEvent(new TeamActivated());
                }
                else if (_stateMachine.CanFire(TeamTrigger.Deactivate))
                {
                    RaiseEvent(new TeamDeactivated());
                }
            }
        }

        public void UpdateIncomingWebhookUrl(Uri incomingWebhookUrl)
        {
            if (incomingWebhookUrl != null)
            {
                if (!incomingWebhookUrl.IsAbsoluteUri)
                {
                    throw new ArgumentException("The incoming webhook url must be absolute.");
                }
                if (string.IsNullOrWhiteSpace(incomingWebhookUrl.Scheme) ||
                    !incomingWebhookUrl.Scheme.StartsWith("http") ||
                    string.IsNullOrWhiteSpace(incomingWebhookUrl.Host))
                {
                    throw new ArgumentException("The incoming webhook url is not valid.");
                }
            }
            if (!Uri.Equals(incomingWebhookUrl, _incomingWebhookUrl))
            {
                RaiseEvent(new TeamIncomingWebhookUpdated { IncomingWebhookUrl = incomingWebhookUrl });
                if (_stateMachine.CanFire(TeamTrigger.Activate))
                {
                    RaiseEvent(new TeamActivated());
                }
                else if (_stateMachine.CanFire(TeamTrigger.Deactivate))
                {
                    RaiseEvent(new TeamDeactivated());
                }
            }
        }

        public void UpdateInfo(string name, Uri slackUrl)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }
            if (slackUrl == null)
            {
                throw new ArgumentNullException("slackUrl");
            }
            name = name.Trim();
            if (!string.Equals(_name, name, StringComparison.Ordinal) ||
                _slackUrl != slackUrl)
            {
                RaiseEvent(new TeamInfoUpdated
                {
                    Name = name,
                    SlackUrl = slackUrl
                });
            }
        }

        protected override void ApplyEventCore(IDomainEvent @event)
        {
            Apply((dynamic)@event);
        }

        private void Apply(TeamCreated @event)
        {
            _stateMachine.Fire(_createTrigger, @event.Id);
        }

        private void Apply(TeamSlashCommandTokenUpdated @event)
        {
            _slashCommandToken = @event.SlashCommandToken;
        }

        private void Apply(TeamIncomingWebhookUpdated @event)
        {
            _incomingWebhookUrl = @event.IncomingWebhookUrl;
        }

        private void Apply(TeamActivated @event)
        {
            _stateMachine.Fire(TeamTrigger.Activate);
        }

        private void Apply(TeamDeactivated @event)
        {
            _stateMachine.Fire(TeamTrigger.Deactivate);
        }

        private void Apply(TeamInfoUpdated @event)
        {
            _name = @event.Name;
            _slackUrl = @event.SlackUrl;
        }

        private void ConfigureStateMachine()
        {
            _stateMachine = new StateMachine<TeamState, TeamTrigger>(
                () => _state,
                state => _state = state
            );
            _createTrigger = _stateMachine.SetTriggerParameters<string>(TeamTrigger.Create);

            _stateMachine.Configure(TeamState.Initial)
                .Permit(TeamTrigger.Create, TeamState.Created);
            _stateMachine.Configure(TeamState.Created)
                .PermitReentry(TeamTrigger.Deactivate)
                .OnEntryFrom(_createTrigger, id =>
                {
                    Id = id;
                })
                .PermitIf(TeamTrigger.Activate, TeamState.Active, () => HasValidConfiguration);
            _stateMachine.Configure(TeamState.Active)
                .PermitIf(TeamTrigger.Deactivate, TeamState.Created, () => !HasValidConfiguration);
        }

        private enum TeamState
        {
            Initial,
            Created,
            Active
        }

        private enum TeamTrigger
        {
            Create,
            Activate,
            Deactivate
        }
    }
}
