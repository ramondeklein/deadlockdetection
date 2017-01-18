using System;

namespace DeadlockDetection
{
    /// <summary>
    /// Global settings for the deadlock detection
    /// </summary>
    public static class GlobalSettings
    {
        /// <summary>
        /// Get or sets the default deadlock detection mode.
        /// </summary>
        public static DeadlockDetectionMode DefaultDetectionMode { get; set; } = DeadlockDetectionMode.OnlyActualDeadlocks;

        /// <summary>
        /// Flag indicating whether or not stack traces are included in the
        /// <see cref="DeadlockException"/> of the blocking operation.
        /// </summary>
        /// <remarks>
        /// Although it is useful to have the stack trace available, it adds
        /// performance overhead to the deadlock detection, so the default
        /// setting is to disable it. You might consider to enable it when
        /// running inside the debugger and/or DEBUG mode.
        /// </remarks>
        public static bool GenerateStackTraces { get; set; } = false;

        /// <summary>
        /// Get or sets the duration after which a blocking operation will
        /// show a warning message about a potential deadlock.
        /// </summary>
        /// <remarks>
        /// This duration is only  used when deadlock detection is enabled,
        /// but no deadlock synchronization context is enabled. You might
        /// increase the duration if you have long running tasks.
        /// </remarks>
        public static TimeSpan BlockingWarningDuration = TimeSpan.FromSeconds(10);
    }
}
