using System;
using System.Collections.Generic;
using Perpetuum.Log;
using Perpetuum.Threading.Process;

namespace Perpetuum.Zones.Terrains.Materials.Minerals
{
    public abstract class ActiveLayer : Layer,IProcess
    {
        protected ActiveLayer(LayerType layerType, int width, int height) : base(layerType, width, height)
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public virtual void Update(TimeSpan time)
        {
            if (_actions.Count <= 0)
                return;

            ProcessLayerActions();
        }

        private readonly Queue<ILayerAction> _actions = new Queue<ILayerAction>();

        public void RunAction(ILayerAction action)
        {
            lock (_actions)
            {
                _actions.Enqueue(action);
            }
        }

        private void ProcessLayerActions()
        {
            while (true)
            {
                ILayerAction action;
                lock (_actions)
                {
                    if (_actions.Count == 0)
                        return;

                    action = _actions.Dequeue();
                }

                try
                {
                    action.Execute(this);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }
    }
}