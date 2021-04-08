using System;
using Perpetuum.Log;
using Perpetuum.Services.Sessions;

namespace Perpetuum.Services.Relay
{
    public class RelayStateService : IRelayStateService
    {
        private readonly RelayInfoBuilder.Factory _relayInfoBuilderFactory;
        private RelayState _state;

        public RelayStateService(RelayInfoBuilder.Factory relayInfoBuilderFactory)
        {
            _relayInfoBuilderFactory = relayInfoBuilderFactory;
            _state = RelayState.OpenForPublic;
        }

        public RelayState State
        {
            get => _state;
            set
            {
                if ( _state == value )
                    return;

                _state = value;

                StateChanged?.Invoke(_state);

                Logger.Info($"Relay state = {_state}");

                SendStateToAll();
            }
        }

        public event Action<RelayState> StateChanged;

        private void SendStateToAll()
        {
            CreateStateMessageBuilder().ToAll().Send();
        }

        public void SendStateToClient(ISession session)
        {
            CreateStateMessageBuilder().ToClient(session).Send();
        }

        private MessageBuilder CreateStateMessageBuilder()
        {
            var builder = _relayInfoBuilderFactory();
            var info = builder.Build();
            return Message.Builder.SetCommand(Commands.State).WithData(info.ToDictionary());
        }

        public void ConfigOnlyAllowAdmins(bool enabled)
        {
            var factory = _relayInfoBuilderFactory();
            factory.ConfigOnlyAllowAdmins(enabled);
        }
    }
}