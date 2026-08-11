using System;
using System.Threading;

namespace HtmlTextBlock.Tests
{
    /// <summary>
    /// WPF Inline/TextBlock objects are DispatcherObjects; running assertions against them
    /// from an arbitrary thread pool thread is unreliable, so tests that touch them run their
    /// body on a dedicated STA thread via this helper instead of pulling in a WPF test runner
    /// package just for that.
    /// </summary>
    internal static class StaThread
    {
        public static void Run(Action action)
        {
            Exception? thrown = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { thrown = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (thrown != null)
                throw thrown;
        }
    }
}
