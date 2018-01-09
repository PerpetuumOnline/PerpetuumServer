using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Log;
using Perpetuum.Services.Sessions;
using Perpetuum.Timers;

namespace Perpetuum.Services.Relay
{
    public class LoginQueueService : ILoginQueueService
    {
        private readonly ISessionManager _sessionManager;
        private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(5));
        private Queue<SignInInfo> _signInInfos = new Queue<SignInInfo>();
        private readonly object _queueSync = new object();

        public LoginQueueService(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        private class SignInInfo
        {
            public readonly ISession session;
            public readonly int accountId;
            public readonly string hwHash;

            public SignInInfo(ISession session, int accountId, string hwHash)
            {
                this.accountId = accountId;
                this.session = session;
                this.hwHash = hwHash.IsNullOrEmpty() ? "hash_" + FastRandom.NextString(7) : hwHash.Substring(0, Math.Min(50, hwHash.Length));
            }
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Update(TimeSpan time)
        {
            _timer.Update(time).IsPassed(RefreshQueue);
        }

        public void EnqueueAccount(ISession session, int accountID, string hwHash)
        {
            var info = new SignInInfo(session,accountID,hwHash);

            lock (_queueSync)
            {
                _signInInfos.Enqueue(info);
                SendQueueInfoToWaitingClients();
            }
        }

        private void RefreshQueue()
        {
            lock (_queueSync)
            {
                DequeueAccountInfo();

                if (_signInInfos.Count == 0)
                    return;

                var newQueue = new Queue<SignInInfo>();

                while (_signInInfos.TryDequeue(out SignInInfo info))
                {
                    newQueue.Enqueue(info);
                }

                _signInInfos = newQueue;
                SendQueueInfoToWaitingClients();
            }
        }

        private void DequeueAccountInfo()
        {
            var sendInfo = false;

            try
            {
                while (_sessionManager.HasFreeSlot() && _signInInfos.TryDequeue(out SignInInfo info))
                {
                    sendInfo = true;

                    try
                    {
                        info.session.SignIn(info.accountId,info.hwHash);
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex);
                    }
                }
            }
            finally
            {
                if (sendInfo)
                {
                    SendQueueInfoToWaitingClients();
                }
            }
        }

        private void SendQueueInfoToWaitingClients()
        {
            var q = _signInInfos.ToArray();
            if (q.Length == 0)
                return;

            var messageBuilder = Message.Builder.SetCommand(new Command("signInQueueInfo")).SetData("length", q.Length);

            foreach (var queueInfo in q.Select((info, currentPosition) => new { info, currentPosition }))
            {
                messageBuilder.SetData("current", queueInfo.currentPosition).ToClient(queueInfo.info.session).Send();
            }
        }
    }
}