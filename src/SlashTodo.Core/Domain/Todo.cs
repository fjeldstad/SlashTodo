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
            throw new NotImplementedException();
        }

        public void Free(bool force = false)
        {
            throw new NotImplementedException();
        }

        public void Tick(bool force = false)
        {
            throw new NotImplementedException();
        }

        public void Untick()
        {
            throw new NotImplementedException();
        }

        public void Remove(bool force = false)
        {
            throw new NotImplementedException();
        }

        protected override void RaiseEvent(IDomainEvent @event)
        {
            if (Context == null)
            {
                throw new InvalidOperationException("Context is null.");
            }
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
