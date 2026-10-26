using System;
using System.Threading;

namespace DataSenderPlusProject.Helpers
{
    // Acquired and disposed on the application's startup/UI thread.
    internal sealed class SingleInstanceGuard : IDisposable
    {
        private readonly string name;
        private Mutex mutex;
        private bool ownsMutex;

        public SingleInstanceGuard(string name)
        {
            this.name = name;
        }

        public bool TryAcquire()
        {
            if (ownsMutex) return true;
            if (mutex == null) mutex = new Mutex(false, name);
            try
            {
                ownsMutex = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                // The previous process crashed; this thread now owns the mutex.
                ownsMutex = true;
            }
            return ownsMutex;
        }

        public void Dispose()
        {
            if (ownsMutex)
            {
                mutex.ReleaseMutex();
                ownsMutex = false;
            }
            if (mutex != null)
            {
                mutex.Dispose();
                mutex = null;
            }
        }
    }
}
