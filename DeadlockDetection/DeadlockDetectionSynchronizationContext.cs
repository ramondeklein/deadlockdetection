using System;
using System.Diagnostics;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace DeadlockDetection
{
    [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.ControlPolicy | SecurityPermissionFlag.ControlEvidence)]
    internal class DeadlockDetectionSynchronizationContext : SynchronizationContext
    {
        private readonly object _sync = new object();
        private bool _isBlocking;
        private StackTrace _blockingStacktrace;

        public DeadlockDetectionSynchronizationContext(SynchronizationContext baseSynchronizationContext)
        {
            // Save the underlying synchronization context
            BaseSynchronizationContext = baseSynchronizationContext;

            // We do want to have wait notifications
            SetWaitNotificationRequired();
        }

        public SynchronizationContext BaseSynchronizationContext { get; }

        public override void Post(SendOrPostCallback d, object state)
        {
            lock (_sync)
            {
                // If we are already blocking, then posting to the synchronization
                // context will (potentially) block the operation.
                if (_isBlocking)
                    throw new DeadlockException(_blockingStacktrace, BaseSynchronizationContext != null);
            }

            // Post the actual completion method, so it will be executed
            BaseSynchronizationContext.Post(d, state);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (BaseSynchronizationContext != null)
                BaseSynchronizationContext.Send(d, state);
            else
                base.Send(d, state);
        }

        public override void OperationStarted()
        {
            if (BaseSynchronizationContext != null)
                BaseSynchronizationContext.OperationStarted();
            else
                base.OperationStarted();
        }

        public override void OperationCompleted()
        {
            if (BaseSynchronizationContext != null)
                BaseSynchronizationContext.OperationCompleted();
            else
                base.OperationCompleted();
        }

        [SecurityCritical, CLSCompliant(false)]
        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            // We cannot block multiple times at once
            Debug.Assert(!_isBlocking);
            try
            {
                lock (_sync)
                {
                    _isBlocking = true;
                    if (GlobalSettings.GenerateStackTraces)
                        _blockingStacktrace = new StackTrace();
                }
                var waitContext = BaseSynchronizationContext ?? this;
                return waitContext.Wait(waitHandles, waitAll, millisecondsTimeout);
            }
            finally
            {
                _isBlocking = false;
            }
        }

        public override SynchronizationContext CreateCopy()
        {
            var copy = new DeadlockDetectionSynchronizationContext(BaseSynchronizationContext?.CreateCopy());
            lock (_sync)
            {
                copy._isBlocking = _isBlocking;
                copy._blockingStacktrace = _blockingStacktrace;
            }
            return copy;
        }
    }
}