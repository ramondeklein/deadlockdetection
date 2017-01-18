using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DeadlockDetection
{
    public static class TaskHelper
    {
        /// <summary>
        /// Waits for the <see cref="Task"/> to complete execution with deadlock detection.
        /// </summary>
        /// <param name="task">
        /// The <see cref="Task"/> to wait for.
        /// </param>
        /// <exception cref="AggregateException">
        /// The <see cref="Task"/> was canceled -or- an exception was thrown during
        /// the execution of the <see cref="Task"/>.
        /// </exception>
        public static void SafeWait(this Task task)
        {
            RunBlocking(task.Wait);
        }

        /// <summary>
        /// Waits for the <see cref="Task"/> to complete execution with deadlock detection.
        /// </summary>
        /// <param name="task">
        /// The <see cref="Task"/> to wait for.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for the task to complete.
        /// </param>
        /// <exception cref="OperationCanceledException">
        /// The <paramref name="cancellationToken"/> was canceled.
        /// </exception>
        /// <exception cref="AggregateException">
        /// The <see cref="Task"/> was canceled -or- an exception was thrown during the execution of the
        /// <see cref="Task"/>.
        /// </exception>
        public static void SafeWait(this Task task, CancellationToken cancellationToken)
        {
            RunBlocking(() => task.Wait(cancellationToken));
        }

        /// <summary>
        /// Waits for the <see cref="Task"/> to complete execution with deadlock detection.
        /// </summary>
        /// <param name="task">
        /// The <see cref="Task"/> to wait for.
        /// </param>
        /// <param name="timeout">
        /// A <see cref="System.TimeSpan"/> that represents the number of milliseconds to wait, or a
        /// <see cref="System.TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <returns>
        /// true if the <see cref="Task"/> completed execution within the allotted time; otherwise, false.
        /// </returns>
        /// <exception cref="AggregateException">
        /// The <see cref="Task"/> was canceled -or- an exception was thrown during the execution of the 
        /// <see cref="Task"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="timeout"/> is a negative number other than -1 milliseconds, which represents an
        /// infinite time-out -or- timeout is greater than <see cref="int.MaxValue"/>.
        /// </exception>
        public static void SafeWait(this Task task, TimeSpan timeout)
        {
            RunBlocking(() => task.Wait(timeout));
        }

        /// <summary>
        /// Waits for the <see cref="Task"/> to complete execution with deadlock detection.
        /// </summary>
        /// <param name="task">
        /// The <see cref="Task"/> to wait for.
        /// </param>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, or <see cref="Timeout.Infinite"/> (-1) to
        /// wait indefinitely.</param>
        /// <returns>true if the <see cref="Task"/> completed execution within the allotted time; otherwise,
        /// false.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="millisecondsTimeout"/> is a negative number other than -1, which represents an
        /// infinite time-out.
        /// </exception>
        /// <exception cref="AggregateException">
        /// The <see cref="Task"/> was canceled -or- an exception was thrown during the execution of the 
        /// <see cref="Task"/>.
        /// </exception>
        public static void SafeWait(this Task task, int millisecondsTimeout)
        {
            RunBlocking(() => task.Wait(millisecondsTimeout));
        }

        /// <summary>
        /// Waits for the <see cref="Task"/> to complete execution with deadlock detection.
        /// </summary>
        /// <param name="task">
        /// The <see cref="Task"/> to wait for.
        /// </param>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, or <see cref="Timeout.Infinite"/> (-1) to
        /// wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        /// true if the <see cref="Task"/> completed execution within the allotted time; otherwise, false.
        /// </returns>
        /// <exception cref="AggregateException">
        /// The <see cref="Task"/> was canceled -or- an exception was thrown during the execution of the 
        /// <see cref="Task"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="millisecondsTimeout"/> is a negative number other than -1, which represents an
        /// infinite time-out.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The <paramref name="cancellationToken"/> was canceled.
        /// </exception>
        public static void SafeWait(this Task task, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            RunBlocking(() => task.Wait(millisecondsTimeout, cancellationToken));
        }

        /// <summary>
        /// Gets the result value of this <see cref="Task{TResult}"/> with deadlock detection.
        /// </summary>
        /// <remarks>
        /// The get accessor for this property ensures that the asynchronous operation is complete before
        /// returning. Once the result of the computation is available, it is stored and will be returned
        /// immediately on later calls to <see cref="Task{TResult}.Result"/>.
        /// </remarks>
        public static TResult SafeResult<TResult>(this Task<TResult> task)
        {
            return RunBlocking(() => task.Result);
        }

        private static T RunBlocking<T>(Func<T> func)
        {
            // If we are running within the deadlock detection synchronization
            // context, then we'll pass the blocking operation to the context,
            // so it can check for deadlocks.
            var deadlockDetectionSynchronizationContext = SynchronizationContext.Current as DeadlockDetectionSynchronizationContext;
            if (deadlockDetectionSynchronizationContext != null)
                return deadlockDetectionSynchronizationContext.RunBlocking(func);

            // Show potential deadlocks that block longer than the specific interval
            using (ShowPotentialDeadlocks())
            {
                return func();
            }
        }

        private static void RunBlocking(Action action)
        {
            RunBlocking(() => { action(); return true; });
        }

        private static IDisposable ShowPotentialDeadlocks()
        {
            // Don't show potential deadlock exceptions, when the global
            // settings don't ask us to do so
            if (GlobalSettings.DefaultDetectionMode != DeadlockDetectionMode.AlsoPotentialDeadlocks)
                return null;

            // Obtain the stack trace of the blocking operation
            var blockingStackTrace = GlobalSettings.GenerateStackTraces ? new StackTrace() : null;
            var cts = new CancellationTokenSource();

            var blockingDuration = GlobalSettings.BlockingWarningDuration;
            // ReSharper disable once MethodSupportsCancellation
            Task.Delay(GlobalSettings.BlockingWarningDuration).ContinueWith((t, o) =>
            {
                if (blockingStackTrace != null)
                    Trace.TraceWarning($"Wait is blocking longer than {blockingDuration} (potential deadlock)...\nStacktrace of blocking operation:\n{blockingStackTrace}");
                else
                    Trace.TraceWarning($"Wait is blocking longer than {blockingDuration} (potential deadlock)");
            }, TaskContinuationOptions.OnlyOnRanToCompletion, cts.Token);
            return cts;
        }
    }
}