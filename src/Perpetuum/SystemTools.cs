using System.Diagnostics;
using System.Linq;

namespace Perpetuum
{
    public static class SystemTools
    {
        /// <summary>
        /// Returns the call stack as string
        /// </summary>
        public static string GetCallStack()
        {
            var method = "";
            var stackTrace = new StackTrace(); // get call stack
            var stackFrames = stackTrace.GetFrames(); // get method calls (frames)

            if (stackFrames != null)
            {
                // write call stack method names
                method = stackFrames.Aggregate(method, (current, stackFrame) => current + (stackFrame.GetMethod().DeclaringType.Name + " : " + stackFrame.GetMethod().Name + "\n"));
            }

            return method;
        }
    }
}
