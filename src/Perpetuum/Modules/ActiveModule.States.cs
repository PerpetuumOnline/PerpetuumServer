using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Items.Ammos;
using Perpetuum.Log;
using Perpetuum.Robots;
using Perpetuum.StateMachines;
using Perpetuum.Timers;

namespace Perpetuum.Modules
{
    public enum ModuleStateType
    {
        Idle = 1,
        Oneshot,
        AutoRepeat,
        Disabled,
        AmmoLoad,
        Shutdown
    }

    public interface IModuleState
    {
        ModuleStateType Type { get; }
        void SwitchTo(ModuleStateType type);
        void LoadAmmo(int ammoDefinition);
        void UnloadAmmo();
    }
    
    public interface ITimedModuleState : IModuleState
    {
        IntervalTimer Timer { get; }
    }

    partial class ActiveModule
    {
        private readonly StackFSM _states = new StackFSM();

        [NotNull]
        public IModuleState State
        {
            get
            {
                var currentState = _states.Current;
                Debug.Assert(currentState != null, "_states != null");
                return (IModuleState) currentState;
            }
        }

        private void InitState()
        {
            _states.StateChanged += OnStateChanged;
            ForceIdleState();
        }

        private void ForceIdleState()
        {
            _states.DirectClear();
            _states.Push(new IdleState(this));
        }

        protected virtual void OnStateChanged(IState state)
        {
            SendModuleStateToPlayer();
        }

        private abstract class ModuleState : IState, IModuleState
        {
            protected ModuleState(ActiveModule module, ModuleStateType type)
            {
                Module = module;
                Type = type;
            }

            protected ActiveModule Module { get; private set; }
            public    ModuleStateType Type { get; private set; }

            public abstract void SwitchTo(ModuleStateType type);
            public abstract void LoadAmmo(int ammoDefinition);
            public abstract void UnloadAmmo();

            public virtual void Enter() { }
            public void Exit() { }

            public abstract void Update(TimeSpan time);

            protected void HandleException(Exception ex)
            {
                var err = ErrorCodes.ServerError;
                var gex = ex as PerpetuumException;
                if (gex != null)
                {
                    err = gex.error;
                }
                else
                {
                    Logger.Exception(ex);
                }

                Module.OnError(err);
            }
        }

        #region Idle State

        private class IdleState : ModuleState
        {
            public IdleState(ActiveModule module) : base(module, ModuleStateType.Idle)
            {
            }

            public override void Update(TimeSpan time)
            {
            }

            public override void SwitchTo(ModuleStateType type)
            {
                switch (type)
                {
                    case ModuleStateType.Oneshot:
                    case ModuleStateType.AutoRepeat:
                    {
                        if (Module.ED.AttributeFlags.ForceOneCycle)
                            type = ModuleStateType.Oneshot;

                        Module._states.Push(new ActiveState(Module, type));
                        break;
                    }
                }
            }

            public override void LoadAmmo(int ammoDefinition)
            {
                Module._states.Push(new AmmoLoadState(Module, ammoDefinition));
            }

            public override void UnloadAmmo()
            {
                Module._states.Push(new AmmoUnloadState(Module));
            }
        }

        #endregion

        #region Active State

        private class ActiveState : ModuleState, ITimedModuleState
        {
            private AmmoHandlerState _nextAmmoHandlerState;
            private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.Zero);

            public ActiveState(ActiveModule module, ModuleStateType type) : base(module, type)
            {
            }

            public override void SwitchTo(ModuleStateType type)
            {
                switch (type)
                {
                    case ModuleStateType.Idle:
                    {
                        Module._states.Pop();
                        break;
                    }
                    case ModuleStateType.Shutdown:
                    {
                        Module._states.Push(new ShutdownState(Module, _timer));
                        break;
                    }
                }
            }

            public override void LoadAmmo(int ammoDefinition)
            {
                _nextAmmoHandlerState = new AmmoLoadState(Module, ammoDefinition);
            }

            public override void UnloadAmmo()
            {
                _nextAmmoHandlerState = new AmmoUnloadState(Module);
            }

            public IntervalTimer Timer
            {
                get { return _timer; }
            }

            public override void Enter()
            {
                ResetTimer();
                base.Enter();
            }

            private void ResetTimer()
            {
                _timer.Interval = Module.CycleTime;
            }

            public override void Update(TimeSpan time)
            {
                if (_timer.Elapsed == TimeSpan.Zero)
                {
                    if (!CheckAmmo())
                    {
                        var loadState = AmmoLoadState.CreateFromCurrentAmmo(Module);
                        if (loadState == null)
                        {
                            Module.OnError(ErrorCodes.AmmoNotFound);
                            SwitchTo(ModuleStateType.Idle);
                            return;
                        }

                        Module._states.Push(loadState);
                        return;
                    }

                    if (!CheckCore())
                    {
                        Module.OnError(ErrorCodes.OutOfCore);
                        SwitchTo(ModuleStateType.Idle);
                        return;
                    }

                    try
                    {
                        Module.OnAction();
                        DecreaseCore();
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex);
                        SwitchTo(ModuleStateType.Idle);
                        return;
                    }
                }

                _timer.Update(time);

                if (!_timer.Passed)
                    return;

                ResetTimer();

                var ammoLoadState = _nextAmmoHandlerState;
                if (ammoLoadState != null)
                {
                    _nextAmmoHandlerState = null;

                    if (Type == ModuleStateType.Oneshot)
                    {
                        Module._states.DirectClear();
                        Module._states.Push(new IdleState(Module));
                    }

                    Module._states.Push(ammoLoadState);
                    return;
                }

                if (Type == ModuleStateType.Oneshot)
                    SwitchTo(ModuleStateType.Idle);
            }

            private void DecreaseCore()
            {
                Debug.Assert(Module.ParentRobot != null, "Module.ParentRobot != null");
                Module.ParentRobot.Core -= Module.CoreUsage;
            }

            private bool CheckAmmo()
            {
                if (!Module.IsAmmoable)
                    return true;

                var ammo = Module.GetAmmo();
                return ammo?.Quantity > 0;
            }

            private bool CheckCore()
            {
                if (Module.CoreUsage <= 0.0)
                    return true;

                Debug.Assert(Module.ParentRobot != null, "Module.ParentRobot != null");
                return Module.ParentRobot.Core >= Module.CoreUsage;
            }
        }

        #endregion

        #region Ammo State

        private abstract class AmmoHandlerState : ModuleState, ITimedModuleState
        {
            private readonly IntervalTimer _timer;

            protected AmmoHandlerState(ActiveModule module) : base(module, ModuleStateType.AmmoLoad)
            {
                Debug.Assert(module.ParentRobot != null, "module.ParentRobot != null");
                _timer = new IntervalTimer(module.ParentRobot.AmmoReloadTime);
            }

            public override void SwitchTo(ModuleStateType type)
            {
            }

            public override void LoadAmmo(int ammoDefinition)
            {
            }

            public override void UnloadAmmo()
            {
            }

            public IntervalTimer Timer
            {
                get { return _timer; }
            }

            private Task _task;

            public override void Update(TimeSpan time)
            {
                if (_task != null)
                {
                    if (!_task.IsCompleted)
                        return;

                    Finish();
                    return;
                }
                
                _timer.Update(time);

                if (!_timer.Passed)
                    return;

                _lastError = ErrorCodes.NoError;
                _task = Task.Run(() => OnAction());
            }

            private void Finish()
            {
                if (_task.Exception == null && _lastError == ErrorCodes.NoError)
                {
                    // nem volt hiba, mehet vissza az elozo statebe
                    Module._states.Pop();
                    return;
                }

                // lekezeljuk az exceptiont
                if (_task.Exception != null)
                    HandleException(_task.Exception.InnerException);

                if (_lastError != ErrorCodes.NoError)
                    Module.OnError(_lastError);

                // le is kapcsoljuk a modult
                Module.ForceIdleState();
            }

            private ErrorCodes _lastError;

            protected void OnError(ErrorCodes error)
            {
                _lastError = error;
            }

            private void OnAction()
            {
                using (var scope = Db.CreateTransaction())
                {
                    Debug.Assert(Module.ParentRobot != null, "Module.ParentRobot != null");
                    var container = Module.ParentRobot.GetContainer();
                    Debug.Assert(container != null, "container != null");
                    container.EnlistTransaction();
                    OnAction(container);

                    Module.Save();
                    container.Save();

                    Transaction.Current.OnCompleted(c =>
                    {
                        Module.UpdateAllProperties();
                        container.SendUpdateToOwner();
                    });

                    scope.Complete();
                }
            }

            protected abstract void OnAction(RobotInventory container);
        }


        private class AmmoUnloadState : AmmoHandlerState
        {
            public AmmoUnloadState(ActiveModule module) : base(module)
            {
            }

            protected override void OnAction(RobotInventory container)
            {
                Module.UnequipAmmoToContainer(container);
            }
        }

        private class AmmoLoadState : AmmoHandlerState
        {
            private readonly int _ammoDefinition;

            public AmmoLoadState(ActiveModule module, int ammoDefinition) : base(module)
            {
                _ammoDefinition = ammoDefinition;
            }

            [CanBeNull]
            public static AmmoLoadState CreateFromCurrentAmmo(ActiveModule module)
            {
                var ammo = module.GetAmmo();
                if (ammo == null || ammo.Definition == 0)
                    return null;

                return new AmmoLoadState(module,ammo.Definition);
            }

            protected override void OnAction(RobotInventory container)
            {
                var currentAmmo = Module.GetAmmo();
                if (currentAmmo != null && currentAmmo.Definition != _ammoDefinition)
                    Module.UnequipAmmoToContainer(container);

                if (_ammoDefinition == 0)
                {
                    OnError(ErrorCodes.AmmoNotFound);
                    return;
                }

                currentAmmo = Module.GetAmmo();
                if (currentAmmo != null)
                {
                    var n = (Module.AmmoCapacity - currentAmmo.Quantity).Clamp(0,Module.AmmoCapacity);
                    if (n == 0)
                        return;

                    var q = container.RemoveItemByDefinition(currentAmmo.Definition, n);
                    if (q == 0)
                    {
                        Module.OnError(ErrorCodes.AmmoNotFound);
                    }
                    
                    currentAmmo.Quantity += q;

                    if (currentAmmo.Quantity <= 0)
                    {
                        Repository.Delete(currentAmmo);
                        Module.SetAmmo(null);
                        return;
                    }

                    Module.SendAmmoUpdatePacketToPlayer();
                }
                else
                {
                    var newAmmo = (Ammo) container.GetAndRemoveItemByDefinition(_ammoDefinition, Module.AmmoCapacity);
                    if (newAmmo == null)
                    {
                        OnError(ErrorCodes.AmmoNotFound);
                    }

                    Module.SetAmmo(newAmmo);
                }
            }
        }

        #endregion

        #region Shutdown State

        private class ShutdownState : ModuleState, ITimedModuleState
        {
            private readonly IntervalTimer _timer;

            public ShutdownState(ActiveModule module, IntervalTimer timer) : base(module, ModuleStateType.Shutdown)
            {
                _timer = timer;
            }

            public override void SwitchTo(ModuleStateType type) { }

            public override void LoadAmmo(int ammoDefinition) { }

            public override void UnloadAmmo() { }

            public IntervalTimer Timer
            {
                get { return _timer; }
            }

            public override void Update(TimeSpan time)
            {
                _timer.Update(time);

                if (_timer.Passed)
                {
                    Module.ForceIdleState();
                }
            }
        }

        #endregion
    }
}
