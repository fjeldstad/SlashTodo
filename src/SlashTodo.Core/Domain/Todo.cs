using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stateless;

namespace SlashTodo.Core.Domain
{
    public class Todo
    {
        private readonly Guid _id;
        private readonly TodoData _data;
        private readonly TodoContext _context;
        private readonly List<ITodoEvent> _pendingEvents = new List<ITodoEvent>(); 
        private StateMachine<TodoState, TodoTrigger> _stateMachine;
        private StateMachine<TodoState, TodoTrigger>.TriggerWithParameters<string> _addTrigger;
        private StateMachine<TodoState, TodoTrigger>.TriggerWithParameters<string> _claimTrigger;

        public Guid Id { get { return _id; } }

        public static Todo Add(Guid id, TodoContext context, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentNullException("text");
            }
            var todo = new Todo(id, context, new TodoData
            {
                State = TodoState.Initial.ToString().ToLowerInvariant()
            });
            todo._stateMachine.Fire(todo._addTrigger, text.Trim());
            return todo;
        }

        public Todo(Guid id, TodoContext context, TodoData data)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            _id = id;
            _data = data;
            _context = context;
            ConfigureStateMachine();
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

        public IEnumerable<ITodoEvent> GetPendingEvents()
        {
            return _pendingEvents.AsEnumerable();
        }

        public void ClearPendingEvents()
        {
            _pendingEvents.Clear();
        }

        internal TodoData GetData()
        {
            return _data;
        }

        private void ConfigureStateMachine()
        {
            if (_data == null)
            {
                throw new InvalidOperationException("Data missing, cannot configure state machine.");
            }

            _stateMachine = new StateMachine<TodoState, TodoTrigger>(
                () => (TodoState)Enum.Parse(typeof(TodoState), _data.State, ignoreCase: true),
                state => _data.State = state.ToString().ToLowerInvariant()
            );
            _addTrigger = _stateMachine.SetTriggerParameters<string>(TodoTrigger.Add);
            _claimTrigger = _stateMachine.SetTriggerParameters<string>(TodoTrigger.Claim);

            _stateMachine.Configure(TodoState.Initial)
                .Permit(TodoTrigger.Add, TodoState.Free);
            _stateMachine.Configure(TodoState.Pending)
                .Permit(TodoTrigger.Tick, TodoState.Done)
                .Permit(TodoTrigger.Remove, TodoState.Removed);
            _stateMachine.Configure(TodoState.Free)
                .SubstateOf(TodoState.Pending)
                .OnEntryFrom(_addTrigger, text =>
                {
                    _data.Text = text;
                    _pendingEvents.Add(new TodoAdded
                    {
                        Id = _id,
                        UserId = _context.UserId,
                        Timestamp = DateTime.UtcNow,
                        Text = _data.Text
                    });
                })
                .Permit(TodoTrigger.Claim, TodoState.Claimed);
            _stateMachine.Configure(TodoState.Claimed)
                .SubstateOf(TodoState.Pending)
                .OnEntryFrom(_claimTrigger, userId => _data.ClaimedBy = userId)
                .PermitReentry(TodoTrigger.Claim)
                .Permit(TodoTrigger.Free, TodoState.Free)
                .OnExit(() => _data.ClaimedBy = null);
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
