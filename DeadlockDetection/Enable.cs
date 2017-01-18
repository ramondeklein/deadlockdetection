using System;
using System.Threading;

namespace DeadlockDetection
{
    public class Enable : IDisposable
    {
        private readonly DeadlockDetectionMode _detectionMode;
        private readonly DeadlockDetectionSynchronizationContext _deadlockDetectionSynchronizationContext;

        /// <summary>
        /// Enable deadlock detection with the default deadlock detection mode.
        /// </summary>
        public static IDisposable DeadlockDetection()
        {
            return DeadlockDetection(GlobalSettings.DefaultDetectionMode);
        }

        /// <summary>
        /// Enable deadlock detection with a specific deadlock detection mode.
        /// </summary>
        /// <param name="detectionMode">Deadlock detection mode.</param>
        public static IDisposable DeadlockDetection(DeadlockDetectionMode detectionMode)
        {
            // Don't do anything if deadlock detection is disabled
            if (detectionMode == DeadlockDetectionMode.Disabled)
                return null;

            // Use deadlock detection
            return new Enable(detectionMode);
        }

        private Enable(DeadlockDetectionMode detectionMode)
        {
            _detectionMode = detectionMode;

            // Determine the current synchronization context and abort if we
            // only want to find actual deadlocks and there is no synchronization context
            var currentSynchronizationContext = SynchronizationContext.Current;
            if (currentSynchronizationContext == null && detectionMode == DeadlockDetectionMode.OnlyActualDeadlocks)
                return;

            // Install our deadlock detection synchronization context
            _deadlockDetectionSynchronizationContext = new DeadlockDetectionSynchronizationContext(currentSynchronizationContext);
            SynchronizationContext.SetSynchronizationContext(_deadlockDetectionSynchronizationContext);
        }

        ~Enable()
        {
            // We should always have been properly disposed, unless deadlock
            // detection has been disabled. No side effects should occur in
            // this mode.
            if (_detectionMode != DeadlockDetectionMode.Disabled)
                throw new InvalidOperationException("Always dispose the deadlock detection (tip: use the 'using' keyword).");
        }

        void IDisposable.Dispose()
        {
            // Don't do anything

            // Restore the original synchronization context if we installed our own
            if (_deadlockDetectionSynchronizationContext != null)
                SynchronizationContext.SetSynchronizationContext(_deadlockDetectionSynchronizationContext.BaseSynchronizationContext);

            // Don't run the finalizer
            GC.SuppressFinalize(this);
        }

    }
}