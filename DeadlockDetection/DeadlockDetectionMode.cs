namespace DeadlockDetection
{
    public enum DeadlockDetectionMode
    {
        /// <summary>
        /// No deadlock detection.
        /// </summary>
        /// <remarks>
        /// No deadlock detection will be used and the code will run exactly
        /// as if the library wasn't used at all. It won't affect performance
        /// or behavior in any way.
        /// </remarks>
        Disabled,

        /// <summary>
        /// Only report actual deadlocks.
        /// </summary>
        /// <remarks>
        /// Only situations that will actually result in a deadlock situation
        /// will be reported (the <see cref="DeadlockException"/> will be
        /// thrown).
        /// </remarks>
        OnlyActualDeadlocks,

        /// <summary>
        /// Also report situation that might deadlock in different situations.
        /// </summary>
        /// <remarks>
        /// Situations that could lead to a deadlock situation when a
        /// <see cref="System.Threading.SynchronizationContext"/> is used will
        /// also be reported when no 
        /// <see cref="System.Threading.SynchronizationContext"/> is active.
        /// This can help you test your library in different circumstances.
        /// </remarks>
        AlsoPotentialDeadlocks
    }
}