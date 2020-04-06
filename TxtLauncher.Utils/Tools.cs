using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TxtLauncher.Utils
{
    public static class Tools
    {
        public static bool None<TItem>(this IEnumerable<TItem> collection)
        {
            if (ReferenceEquals(collection, null))
            {
                throw new ArgumentNullException(nameof(collection));
            }

            return !collection.Any();
        }

        public static bool None<TItem>(this IEnumerable<TItem> collection, Func<TItem, bool> predicate)
        {
            if (ReferenceEquals(collection, null))
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (ReferenceEquals(predicate, null))
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return !collection.Any(predicate);
        }
        
        public static async Task WhenAll(params Task[] tasks)
        {
            await Task.WhenAll(tasks.Where(t => t != null)).ConfigureAwait(false);
        }

        public static async Task WhenAll(IEnumerable<Task> tasks)
        {
            await Task.WhenAll(tasks.Where(t => t != null)).ConfigureAwait(false);
        }

        public static async Task WhenAny(params Task[] tasks)
        {
            await Task.WhenAny(tasks.Where(t => t != null)).ConfigureAwait(false);
        }

        public static async Task WhenAny(IEnumerable<Task> tasks)
        {
            await Task.WhenAny(tasks.Where(t => t != null)).ConfigureAwait(false);
        }

        public static void DispatchedInvoke(Action action, bool useUiThread = true, Dispatcher dispatcher = null)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (dispatcher == null && Application.Current?.Dispatcher == null)
            {
                return;
            }

            dispatcher = dispatcher ?? Application.Current.Dispatcher;

            if (!useUiThread || dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                try
                {
                    if (!dispatcher.HasShutdownStarted)
                    {
                        dispatcher.Invoke(action);
                    }
                }
                catch (OperationCanceledException)
                {
                    // порождается при закрытии приложения
                }
            }
        }

        public static TResult DispatchedInvoke<TResult>(Func<TResult> action, bool useUiThread = true, Dispatcher dispatcher = null)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (dispatcher == null && Application.Current?.Dispatcher == null)
            {
                return default;
            }

            dispatcher = dispatcher ?? Application.Current.Dispatcher;

            if (!useUiThread || dispatcher.CheckAccess())
            {
                return action();
            }

            try
            {
                return !dispatcher.HasShutdownStarted
                    ? dispatcher.Invoke(action)
                    : default;
            }
            catch (OperationCanceledException)
            {
                // порождается при закрытии приложения

                return default;
            }
        }

        public static void CancelAndDispose(ref CancellationTokenSource cts)
        {
            if (!cts?.IsCancellationRequested ?? false)
            {
                cts?.Cancel();
            }

            cts?.Dispose();
            cts = null;
        }

        public static CancellationToken CancelAndRecreate(ref CancellationTokenSource cts, TimeSpan? timeout = null)
        {
            CancelAndDispose(ref cts);

            cts = timeout > TimeSpan.Zero
                ? new CancellationTokenSource(timeout.Value)
                : new CancellationTokenSource();

            return cts.Token;
        }

        public static void DisposeAndClear<TField>(ref TField disposable) where TField : class, IDisposable
        {
            disposable?.Dispose();
            disposable = null;
        }
    }
}
