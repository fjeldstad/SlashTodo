using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stateless;

namespace SlashTodo.Core.Domain
{
    public class Account : Aggregate
    {
        private AccountState _state = AccountState.Initial;
        private StateMachine<AccountState, AccountTrigger> _stateMachine;
        private StateMachine<AccountState, AccountTrigger>.TriggerWithParameters<Guid, string> _createTrigger;
        private string _teamId;
        private string _slashCommandToken;
        private Uri _incomingWebhookUrl;

        protected bool HasValidConfiguration
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_slashCommandToken) &&
                       _incomingWebhookUrl != null;
            }
        }

        public Account()
        {
            ConfigureStateMachine();
        }

        public static Account Create(Guid id, string teamId)
        {
            if (string.IsNullOrWhiteSpace(teamId))
            {
                throw new ArgumentNullException("teamId");
            }
            var account = new Account();
            if (!account._stateMachine.CanFire(AccountTrigger.Create))
            {
                throw new InvalidOperationException();
            }
            account.Id = id;
            account.RaiseEvent(new AccountCreated { TeamId = teamId });
            return account;
        }

        public void UpdateSlashCommandToken(string slashCommandToken)
        {
            if (string.IsNullOrWhiteSpace(slashCommandToken))
            {
                throw new ArgumentNullException("slashCommandToken");
            }
            RaiseEvent(new AccountSlashCommandTokenUpdated { SlashCommandToken = slashCommandToken });
            if (_stateMachine.CanFire(AccountTrigger.Activate))
            {
                RaiseEvent(new AccountActivated());
            }
        }

        public void UpdateIncomingWebhookUrl(Uri incomingWebhookUrl)
        {
            if (incomingWebhookUrl == null)
            {
                throw new ArgumentNullException("incomingWebhookUrl");
            }
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
            RaiseEvent(new AccountIncomingWebhookUpdated { IncomingWebhookUrl = incomingWebhookUrl });
            if (_stateMachine.CanFire(AccountTrigger.Activate))
            {
                RaiseEvent(new AccountActivated());
            }
        }

        protected override void ApplyEventCore(IDomainEvent @event)
        {
            Apply((dynamic)@event);
        }

        private void Apply(AccountCreated @event)
        {
            _stateMachine.Fire(_createTrigger, @event.Id, @event.TeamId);
        }

        private void Apply(AccountSlashCommandTokenUpdated @event)
        {
            _slashCommandToken = @event.SlashCommandToken;
        }

        private void Apply(AccountIncomingWebhookUpdated @event)
        {
            _incomingWebhookUrl = @event.IncomingWebhookUrl;
        }

        private void Apply(AccountActivated @event)
        {
            _stateMachine.Fire(AccountTrigger.Activate);
        }

        private void ConfigureStateMachine()
        {
            _stateMachine = new StateMachine<AccountState, AccountTrigger>(
                () => _state,
                state => _state = state
            );
            _createTrigger = _stateMachine.SetTriggerParameters<Guid, string>(AccountTrigger.Create);

            _stateMachine.Configure(AccountState.Initial)
                .Permit(AccountTrigger.Create, AccountState.Created);
            _stateMachine.Configure(AccountState.Created)
                .OnEntryFrom(_createTrigger, (id, teamId) =>
                {
                    Id = id;
                    _teamId = teamId;
                })
                .PermitIf(AccountTrigger.Activate, AccountState.Active, () => HasValidConfiguration);
        }

        private enum AccountState
        {
            Initial,
            Created,
            Active
        }

        private enum AccountTrigger
        {
            Create,
            Activate
        }
    }
}
