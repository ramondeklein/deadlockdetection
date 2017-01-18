using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeadlockDetection.SampleApp
{
    public static class Program
    {
        static Program()
        {
            // Use deadlock detection with full potential
            GlobalSettings.GenerateStackTraces = true;
            GlobalSettings.DefaultDetectionMode = DeadlockDetectionMode.AlsoPotentialDeadlocks;
        }

        private static async Task TestAsync()
        {
            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} - Starting wait...");
            await Task.Delay(1000).ConfigureAwait(true);    // Use 'true' to make sure we return on the same thread
            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} - Finished wait...");
        }

        public static void Main()
        {
            using (Enable.DeadlockDetection())
            {
                try
                {
                    TestAsync().SafeWait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
