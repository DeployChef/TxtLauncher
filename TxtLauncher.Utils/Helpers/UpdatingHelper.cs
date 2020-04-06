using System;
using System.Threading;
using TxtLauncher.Utils.Interfaces;

namespace TxtLauncher.Utils.Helpers
{
    public class UpdatingHelper : ISupportUpdate
    {
        private readonly Action _beginUpdateAction;
        private readonly Action _endUpdateAction;
        private long _counter;

        public UpdatingHelper(Action beginUpdateAction = null, Action endUpdateAction = null)
        {
            _beginUpdateAction = beginUpdateAction;
            _endUpdateAction = endUpdateAction;
        }

        public UpdatingHelper(ISupportUpdate updateable) : this(updateable.BeginUpdate, updateable.EndUpdate) { }

        public void BeginUpdate()
        {
            if (Interlocked.Increment(ref _counter) == 1)
            {
                _beginUpdateAction?.Invoke();
            }
        }

        public void EndUpdate()
        {
            if (Interlocked.CompareExchange(ref _counter, 0, 0) == 0)
            {
                throw new InvalidOperationException("Объект не находиться в состоянии обновления.");
            }

            if (Interlocked.Decrement(ref _counter) == 0)
            {
                _endUpdateAction?.Invoke();
            }
        }

        public bool IsUpdating => Interlocked.Read(ref _counter) > 0;

    }
}