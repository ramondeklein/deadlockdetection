# DeadlockDetection
A deadlock detection library can be used to track down async/await related
deadlocks in your code with minimal overhead and effort.

## Overview
The async/await pattern greatly simplified writing sequential code that
operates asynchronous. A lot of libraries now use this pattern to allow
code to be more scalable and responsive. Despite of the simplified code,
asynchronous code can still be tricky and a potential source of deadlocks.

Most deadlocks are caused by mixing synchronous and asynchronous code and
not dealing with synchronization contexts. The best practice is to use async
all the way, but this sometimes is not possible. Some examples:

* Constructors, destructors, disposal, event handlers require synchronous
  code. It's often better to rethink your design and check if you can use an
  alternative implementation, but sometimes you can't.

* .NET libraries (or third party libraries) assume your code to be synchronous.
  Starting a Windows service means overriding the OnStart and OnStop methods
  that are synchronous. If you need to call an asynchronous method inside
  these methods, then the easiest option is to use Wait() or Result.

If you need to call [`Task.Wait()`](https://msdn.microsoft.com/en-us/library/system.threading.tasks.task.wait(v=vs.110).aspx)
or [`Task<T>.Result`](https://msdn.microsoft.com/en-us/library/dd321468(v=vs.110).aspx)
then your current thread blocks. If one of the underlying tasks require that it
needs to continue on the same synchronization context, then the application
will deadlock.

A good starting point is to install the [ConfigureAwait Resharper extension](https://github.com/aelij/ConfigureAwaitChecker/),
so you don't forget to explicitly mark each await. If your code doesn't depend
on the synchronization context then it's often best to call
`ConfigureAwait(false)` to prevent deadlocks. If your method is called by external
code then be cautious when using `ConfigureAwait(true)` (or discarding it). It
might deadlock if the caller uses a synchronization context and uses a blocking
operation.

These deadlocks are hard to debug and I have spent several hours debugging
such deadlocks for projects that I work on. To prevent even more work, I thought
of a simple and lightweight library that I could use to find deadlocks earlier
and more easy.

## Example
Let's illustrate the functionality with an example. Suppose we have the
following program that has a potential deadlock:

```
    private async Task TestAsync()
    {
        await Task.Delay(1000);
    }

    public void Test()
    {
        TestAsync().Wait();
    }
```

If this code is run with a synchronization context (i.e. GUI application or an
ASP.NET request handler), then it will block. The `Test` method calls the
asynchronous `TestAsync` method and will block until it completes. However, the
delayed task needs to complete on the thread that is now blocked. We have
encountered a deadlock situation.

These situations can be hard to find and to track down. An application may stop
responding or a service won't respond and it will slowly eat up your system
resources, because it will use more and more threads.

The DeadlockDetection library can help to track down these issues with minimal
effort and overhead. The only thing you need to do is to make sure you inject
a custom synchronization context. This synchronization context will intercept
all task continuations that have require continuation on the synchronization
context.

If the synchronization context is blocked, due to a `Wait`, `Result` or other
blocking operation then it will raise a `DeadlockException` when continuation
is requested on the blocked thread.

The only thing that you need to do is to install the
`DeadlockDetectionSynchronizationContext` on your current thread. It's best
to use the `Enable.DeadlockDetection` method for this purpose. If deadlock
detection is disabled (i.e. for production systems) then it makes sure it
has zero side-effects and no performance penalty.

So this code will now look like this:

```
    private async Task TestAsync()
    {
        await Task.Delay(1000);
    }

    public void Test()
    {
        using (Enable.DeadlockDetection(DeadlockDetectionMode.AlsoPotentialDeadlocks))
        {
            TestAsync().Wait();
        }
    }
```

In this example I have set the detection mode to the most strict mode, so it
will also raise an exception when a potential deadlock might occur. If you run
this code, then the `DeadlockException` will be thrown when the `TestAsync`
method starts awaiting.

Suppose that you are calling external code or libraries, then deadlocks will
still be detected. Only the calling code needs to be modified.

## Settings
There are some global settings that can be set to change the behavior of the
deadlock detection library.

### DeadlockDetection.GlobalSettings.DefaultDetectionMode
This setting specifies how deadlocks are detected:

 * `Disabled` will disable the entire library and the code will run exactly
   as if the library wasn't used at all. It won't affect performance or
   behavior in any way. It's okay to leave the usings (without setting the
   detection mode) in production code this way.
 * `OnlyActualDeadlocks` will only raise exceptions when the deadlock detection
   is explicitly enabled via the `using` block. Potential deadlocks will not be
   detected.
 * `AlsoPotentialDeadlocks` will raise exceptions when both actual or potential
   deadlocks are detected. This should only be enabled during development and/or
   testing.

### DeadlockDetection.GlobalSettings.GenerateStackTraces
This setting specifies if stack traces are generated during the detection of
(potential) deadlocks. Although it is convenient to have the location of the
blocking operation, the generation of stack traces might affect performance,
so it is best to disable it when not running in development and/or testing.

### DeadlockDetection.GlobalSettings.BlockingWarningDuration
The safe deadlock detection methods can also detect long running tasks and
mark them as potential deadlocks (only when deadlock detection mode is set to
`AlsoPotentialDeadlocks`). This setting specifies the duration after which
a warning is emitted to debug output.

## Under the hood
The deadlock detection library uses existing facilities in the
`SynchronizationContext` class to detect deadlocks. That's why I was able to
implement it without any overhead. It uses the following two techniques:

 1. When a task is continued on the synchronization context, then the CLR
    will use the `SynchronizationContext.Post` method to call the continuation
	method.
 2. Synchronization contexts can opt-in to be notified when it blocks (via
    the `SynchronizationContext.SetWaitNotificationRequired` method). It
	calls the `SynchronizationContext.Wait` method to block the thread.

The `DeadlockDetectionSynchronizationContext` opts-in for the wait notification
and sets the `_isBlocked` flag. If the continuation method is invoked via the
`Post` method of our synchronization context, then it will check if the context
is currently blocked and if so, it throws the `DeadlockException`.
