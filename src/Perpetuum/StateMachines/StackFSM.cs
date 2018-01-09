using System;
using System.Collections.Generic;

namespace Perpetuum.StateMachines
{
    public class StackFSM
    {
        private readonly Stack<IState> _states = new Stack<IState>();

        public event Action<IState> StateChanged;

        private void OnStateChanged(IState state)
        {
            StateChanged?.Invoke(state);
        }

        [CanBeNull]
        public IState Current
        {
            get
            {
                if (_states.Count > 0)
                    return _states.Peek();

                return null;
            }
        }

        public void DirectClear()
        {
            _states.Clear();
        }

        public void Clear()
        {
            while (_states.Count > 0)
            {
                var state = _states.Pop();
                state.Exit();
            }
        }

        public void Push(IState state)
        {
            Current?.Exit();

            state.Enter();
            _states.Push(state);

            OnStateChanged(state);
        }

        public IState Pop()
        {
            var state = _states.Pop();
            state.Exit();

            Current?.Enter();

            OnStateChanged(Current);

            return state;
        }

        public void Update(TimeSpan time)
        {
            Current?.Update(time);
        }
    }
}