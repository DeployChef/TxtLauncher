using System;
using System.Threading;
using TxtLauncher.Utils.Interfaces;

namespace TxtLauncher.Utils.Helpers
{
    public static class DualActionsHelper
    {
        public static IDisposable DoStartEndActions(Action begin, Action end)
        {
            begin?.Invoke();

            return new ActionRecord(end);
        }

        public static IDisposable DoStartEndActions(Action end)
        {
            return new ActionRecord(end);
        }

        public static IDisposable EnterReadSection(this ReaderWriterLockSlim locker)
        {
            if (ReferenceEquals(locker, null))
            {
                throw new ArgumentNullException(nameof(locker));
            }

            return DoStartEndActions(locker.EnterReadLock, locker.ExitReadLock);
        }

        public static IDisposable EnterUpgradeableReadSection(this ReaderWriterLockSlim locker)
        {
            if (ReferenceEquals(locker, null))
            {
                throw new ArgumentNullException(nameof(locker));
            }

            return DoStartEndActions(locker.EnterUpgradeableReadLock, locker.ExitUpgradeableReadLock);
        }

        public static IDisposable EnterWriteSection(this ReaderWriterLockSlim locker)
        {
            if (ReferenceEquals(locker, null))
            {
                throw new ArgumentNullException(nameof(locker));
            }

            return DoStartEndActions(locker.EnterWriteLock, locker.ExitWriteLock);
        }

        public static IDisposable DoUpdate(this ISupportUpdate obj)
        {
            if (ReferenceEquals(obj, null))
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return DoStartEndActions(obj.BeginUpdate, obj.EndUpdate);
        }

        private class ActionRecord : IDisposable
        {
            private readonly Action _action;

            public ActionRecord(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action?.Invoke();
            }
        }
    }

}
