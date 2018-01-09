using Perpetuum.Services.Sessions;
using Perpetuum.Threading.Process;

namespace Perpetuum.Services.Relay
{
    public interface ILoginQueueService : IProcess
    {
        void EnqueueAccount(ISession session, int accountID, string hwHash);
    }
}