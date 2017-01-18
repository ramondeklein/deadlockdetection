using System;
using System.Diagnostics;
using System.Text;

namespace DeadlockDetection
{
    /// <summary>
    /// Reports a (potential) deadlock situation.
    /// </summary>
    public class DeadlockException : SystemException
    {
        /// <summary>
        /// Stack trace where the program initiated the blocking operation.
        /// </summary>
        /// <remarks>
        /// The stack trace is only set, when the 
        /// <see cref="GlobalSettings.GenerateStackTraces"/> flag has
        /// been set.
        /// </remarks>
        public StackTrace BlockingStackTrace { get; }

        /// <summary>
        /// Flag indicating whether the deadlock is a potential deadlock or an
        /// actual deadlock situation.
        /// </summary>
        public bool IsPotentialDeadlock { get; }

        internal DeadlockException(StackTrace blockingStackTrace, bool isPotentialDeadlock) 
            : base(GetMessage(blockingStackTrace, isPotentialDeadlock))
        {
            BlockingStackTrace = blockingStackTrace;
            IsPotentialDeadlock = isPotentialDeadlock;
        }

        private static string GetMessage(StackTrace blockingStackTrace, bool isPotentialDeadlock)
        {
            // Generate the proper message
            var sb = new StringBuilder();
            sb.AppendLine(isPotentialDeadlock ? 
                "The blocking operation encountered a potential deadlock." : 
                "The blocking operation encountered a deadlock.");

            // Append stack trace of the blocking point (if any)
            if (blockingStackTrace != null)
            {
                sb.AppendLine();
                sb.AppendLine("Stack trace where the blocking operation started:");
                sb.Append(blockingStackTrace);
            }

            // Return message
            return sb.ToString();
        }
    }
}