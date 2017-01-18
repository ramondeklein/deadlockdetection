using System;
using System.Diagnostics;
using System.Threading;

namespace DeadlockDetection
{
    internal class DeadlockDetectionSynchronizationContext : SynchronizationContext
    {
        private bool _isBlocking;
        private StackTrace _blockingStacktrace;

        public DeadlockDetectionSynchronizationContext(SynchronizationContext baseSynchronizationContext)
        {
            // Save the underlying synchronization context
            BaseSynchronizationContext = baseSynchronizationContext;
        }

        public SynchronizationContext BaseSynchronizationContext { get; }

        public override void Post(SendOrPostCallback d, object state)
        {
            // If we are already blocking, then posting to the synchronization
            // context will (potentially) block the operation.
            if (_isBlocking)
                throw new DeadlockException(_blockingStacktrace, BaseSynchronizationContext == null);

            // Post the actual completion method, so it will be executed
            BaseSynchronizationContext.Post(d, state);
        }

        public T RunBlocking<T>(Func<T> func)
        {
            // We cannot block multiple times at once
            Debug.Assert(!_isBlocking);
            try
            {
                _isBlocking = true;
                if (GlobalSettings.GenerateStackTraces)
                    _blockingStacktrace = new StackTrace();
                return func();
            }
            finally
            {
                _isBlocking = false;
            }
        }
    }
}