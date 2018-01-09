using System;

namespace Perpetuum.StateMachines
{
    public interface IState
    {
        void Enter();
        void Exit();
        void Update(TimeSpan time);
    }

    public class AnonymousState : IState
    {
        private readonly Action _onEnter;
        private readonly Action _onExit;
        private readonly Action<TimeSpan> _onUpdate;

        public AnonymousState(Action onEnter, Action onExit, Action<TimeSpan> onUpdate)
        {
            _onEnter = onEnter;
            _onExit = onExit;
            _onUpdate = onUpdate;
        }

        public void Enter()
        {
            _onEnter();
        }

        public void Exit()
        {
            _onExit();
        }

        public void Update(TimeSpan time)
        {
            _onUpdate(time);
        }

        public static AnonymousState Create(Action<TimeSpan> onUpdate)
        {
            return new AnonymousState(Stubs.None,Stubs.None,onUpdate);
        }
    }


}