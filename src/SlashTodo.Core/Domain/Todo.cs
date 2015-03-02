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
        private StateMachine<TodoState, TodoTrigger>.TriggerWithParameters<Guid, string> _addTrigger;
        private StateMachine<TodoState, TodoTrigger>.TriggerWithParameters<string> _claimTrigger;
        private string _text;
        private string _claimedBy;

        public TodoContext Context { get; set; }

        public Todo()
        {
            ConfigureStateMachine();
        }

        public static Todo Add(Guid id, TodoContext context, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentNullException("text");
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
                if (_claimedBy.Equals(Context.UserId))
                {
                    return;
                }
                if (!force)
                {
                    throw new TodoClaimedBySomeoneElseException(_claimedBy);
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
                if (!_claimedBy.Equals(Context.UserId) && 
                    !force)
                {
                    throw new TodoClaimedBySomeoneElseException(_claimedBy);
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
                if (!_claimedBy.Equals(Context.UserId) &&
                    !force)
                {
                    throw new TodoClaimedBySomeoneElseException(_claimedBy);
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
                if (!_claimedBy.Equals(Context.UserId) &&
                    !force)
                {
                    throw new TodoClaimedBySomeoneElseException(_claimedBy);
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
            ((TodoEvent)@event).ConversationId = Context.ConversationId;
            ((TodoEvent)@event).UserId = Context.UserId;
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
            _addTrigger = _stateMachine.SetTriggerParameters<Guid, string>(TodoTrigger.Add);
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
                .OnEntryFrom(_claimTrigger, userId => _claimedBy = userId)
                .PermitReentry(TodoTrigger.Claim)
                .Permit(TodoTrigger.Free, TodoState.Free)
                .OnExit(() => _claimedBy = null);
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
