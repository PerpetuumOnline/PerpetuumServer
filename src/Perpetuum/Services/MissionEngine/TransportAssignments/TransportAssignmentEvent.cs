namespace Perpetuum.Services.MissionEngine.TransportAssignments
{
    /// <summary>
    /// Transport assignment event
    /// </summary>
    public enum TransportAssignmentEvent
    {
        submit,
        cancel,
        take,
        deliver,
        expired,
        failed,
        retrieved,
        targetBaseDeleted,
        containerRetrieved,
        gaveUp,
    }
}