using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stateless;

namespace SlashTodo.Core.Domain
{
    public class Todo : Aggregate
    {
        private TodoState _state = TodoState.Initial;
        private StateMachine<TodoState, TodoTrigger> _stateMachine;
        private StateMachine<TodoState, TodoTrigger>.TriggerWithParameters<string, string> _addTrigger;
        private StateMachine<TodoState, TodoTrigger>.TriggerWithParameters<string> _claimTrigger;
        private string _text;
        private string _slackConversationId;
        private string _shortCode;
        private string _claimedByUserId;

        public TodoContext Context { get; set; }
        public string Text { get { return _text; } }
        public string SlackConversationId { get { return _slackConversationId; } }
        public string ShortCode { get { return _shortCode; } }
        public string ClaimedByUserId { get { return _claimedByUserId; } }

        public Todo()
        {
            ConfigureStateMachine();
        }

        public static Todo Add(string id, string text, string slackConversationId, string shortCode, TodoContext context)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentNullException("text");
            }
            if (string.IsNullOrWhiteSpace(slackConversationId))
            {
                throw new ArgumentNullException("slackConversationId");
            }
            if (string.IsNullOrWhiteSpace(shortCode))
            {
                throw new ArgumentNullException("shortCode");
            }
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            var todo = new Todo
            {
                Context = context
            };
            if (!todo._stateMachine.CanFire(TodoTrigger.Add))
            {
                throw new InvalidOperationException();
            }
            todo.Id = id;
            todo._slackConversationId = slackConversationId;
            todo._shortCode = shortCode;
            todo.RaiseEvent(new TodoAdded { Text = text.Trim() });
            return todo;
        }

        public void Claim(bool force = false)
        {
            if (!_stateMachine.CanFire(TodoTrigger.Claim))
            {
                return;
            }
            if (_stateMachine.IsInState(TodoState.Claimed))
            {
                if (string.Equals(_claimedByUserId, Context.UserId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (!force)
                {
                    throw new TodoClaimedBySomeoneElseException(_claimedByUserId);
                }
            }
            RaiseEvent(new TodoClaimed());
        }

        public void Free(bool force = false)
        {
            if (!_stateMachine.CanFire(TodoTrigger.Free))
            {
                return;
            }
            if (_stateMachine.IsInState(TodoState.Claimed))
            {
                if (!string.Equals(_claimedByUserId, Context.UserId, StringComparison.OrdinalIgnoreCase) && 
                    !force)
                {
                    throw new TodoClaimedBySomeoneElseException(_claimedByUserId);
                }
            }
            RaiseEvent(new TodoFreed());
        }

        public void Tick(bool force = false)
        {
            if (!_stateMachine.CanFire(TodoTrigger.Tick))
            {
                return;
            }
            if (_stateMachine.IsInState(TodoState.Claimed))
            {
                if (!string.Equals(_claimedByUserId, Context.UserId, StringComparison.OrdinalIgnoreCase) &&
                    !force)
                {
                    throw new TodoClaimedBySomeoneElseException(_claimedByUserId);
                }
            }
            RaiseEvent(new TodoTicked());
        }

        public void Untick()
        {
            if (_stateMachine.CanFire(TodoTrigger.Untick))
            {
                RaiseEvent(new TodoUnticked());
            }
        }

        public void Remove(bool force = false)
        {
            if (!_stateMachine.CanFire(TodoTrigger.Remove))
            {
                return;
            }
            if (_stateMachine.IsInState(TodoState.Claimed))
            {
                if (!string.Equals(_claimedByUserId, Context.UserId, StringComparison.OrdinalIgnoreCase) &&
                    !force)
                {
                    throw new TodoClaimedBySomeoneElseException(_claimedByUserId);
                }
            }
            RaiseEvent(new TodoRemoved());
        }

        protected override void RaiseEvent(IDomainEvent @event)
        {
            if (Context == null)
            {
                throw new InvalidOperationException("Context is null.");
            }
            ((TodoEvent)@event).TeamId = Context.TeamId;
            ((TodoEvent)@event).UserId = Context.UserId;
            ((TodoEvent)@event).SlackConversationId = _slackConversationId;
            ((TodoEvent)@event).ShortCode = _shortCode;
            base.RaiseEvent(@event);
        }

        protected override void ApplyEventCore(IDomainEvent @event)
        {
            Apply((dynamic)@event);
        }

        private void Apply(TodoAdded @event)
        {
            _stateMachine.Fire(_addTrigger, @event.Id, @event.Text);
        }

        private void Apply(TodoClaimed @event)
        {
            _stateMachine.Fire(_claimTrigger, @event.UserId);
        }

        private void Apply(TodoFreed @event)
        {
            _stateMachine.Fire(TodoTrigger.Free);
        }

        private void Apply(TodoTicked @event)
        {
            _stateMachine.Fire(TodoTrigger.Tick);
        }

        private void Apply(TodoUnticked @event)
        {
            _stateMachine.Fire(TodoTrigger.Untick);
        }

        private void Apply(TodoRemoved @event)
        {
            _stateMachine.Fire(TodoTrigger.Remove);
        }

        private void ConfigureStateMachine()
        {
            _stateMachine = new StateMachine<TodoState, TodoTrigger>(
                () => _state,
                state => _state = state
            );
            _addTrigger = _stateMachine.SetTriggerParameters<string, string>(TodoTrigger.Add);
            _claimTrigger = _stateMachine.SetTriggerParameters<string>(TodoTrigger.Claim);

            _stateMachine.Configure(TodoState.Initial)
                .Permit(TodoTrigger.Add, TodoState.Free);
            _stateMachine.Configure(TodoState.Pending)
                .Permit(TodoTrigger.Tick, TodoState.Done)
                .Permit(TodoTrigger.Remove, TodoState.Removed);
            _stateMachine.Configure(TodoState.Free)
                .SubstateOf(TodoState.Pending)
                .OnEntryFrom(_addTrigger, (id, text) =>
                {
                    Id = id;
                    _text = text;
                })
                .Permit(TodoTrigger.Claim, TodoState.Claimed);
            _stateMachine.Configure(TodoState.Claimed)
                .SubstateOf(TodoState.Pending)
                .OnEntryFrom(_claimTrigger, userId => _claimedByUserId = userId)
                .PermitReentry(TodoTrigger.Claim)
                .Permit(TodoTrigger.Free, TodoState.Free)
                .OnExit(() => _claimedByUserId = null);
            _stateMachine.Configure(TodoState.Done)
                .Permit(TodoTrigger.Untick, TodoState.Free)
                .Permit(TodoTrigger.Remove, TodoState.Removed);
        }

        private enum TodoState
        {
            Initial,
            Pending,
            Free,
            Claimed,
            Done,
            Removed
        }

        private enum TodoTrigger
        {
            Add,
            Claim,
            Free,
            Tick,
            Untick,
            Remove
        }
    }
}
