using System;
using System.Threading;

namespace TxtLauncher.Utils
{
    public class CancellationTokenSourceEx : IDisposable
    {
        private readonly object _sync = new object();

        private CancellationTokenSource _cts;
        private bool _isDisposed;

        public CancellationToken Token => AssertOperation(() => _cts?.Token ?? Restart());

        public bool IsActive => AssertOperation(() => _cts != null);

        public void Cancel() => AssertOperation(() => Tools.CancelAndDispose(ref _cts));

        public void Finish() => AssertOperation(() => Tools.DisposeAndClear(ref _cts));

        public CancellationToken Restart(TimeSpan? timeout = null) => AssertOperation(() => Tools.CancelAndRecreate(ref _cts, timeout));

        private void AssertOperation(Action action = null)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(CancellationTokenSourceEx));
            }

            lock (_sync)
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(CancellationTokenSourceEx));
                }

                action?.Invoke();
            }
        }

        private TResult AssertOperation<TResult>(Func<TResult> function = null)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(CancellationTokenSourceEx));
            }

            lock (_sync)
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(CancellationTokenSourceEx));
                }

                return function == null
                           ? default(TResult)
                           : function();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            lock (_sync)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;

                Tools.CancelAndDispose(ref _cts);
            }
        }
    }
}
