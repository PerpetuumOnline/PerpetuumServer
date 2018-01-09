using System;
using System.Collections.Generic;

namespace Perpetuum.Timers
{
    /// <summary>
    /// Handler class for multiple timers 
    /// </summary>
    public class TimerList
    {
        private readonly List<TimerAction> _actions = new List<TimerAction>();

        public void Add(TimerAction action)
        {
            _actions.Add(action);
        }

        public void Update(TimeSpan time)
        {
            foreach (var action in _actions)
            {
                action.Update(time);
            }
        }
    }
}