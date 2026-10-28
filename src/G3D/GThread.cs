/** 
  @file GThread.h
 
  @created 2005-09-22
  @edited  2005-10-22
 */
using System;
using System.Threading;

namespace G3D
{
    /**
        GThread's documentation
    */
    public abstract class GThread
    {
        private sealed class GThreadPrivate
        {
            public bool running;
            public bool completed;

            public GThreadPrivate()
            {
                running = false;
                completed = false;
            }
        }

        private readonly GThreadPrivate pthread;
        private Thread handle;
        private readonly string _name;

        public GThread(string name)
        {
            handle = null;
            _name = name;
            pthread = new GThreadPrivate();
        }

        ~GThread()
        {
            Debug.alwaysAssertM(!pthread.running, "Deleting thread while running.");
        }

        /** Constructs a basic GThread without requiring a subclass.

            @param proc The global or static function for the threadMain() */
        public static GThread create(string name, Action proc)
        {
            return new BasicThread(name, proc);
        }

        /** Starts the thread and executes main() */
        public bool start()
        {
            Debug.debugAssertM(!pthread.completed, "Thread has already executed.");

            if (pthread.completed)
            {
                return false;
            }

            try
            {
                handle = new Thread(new ThreadStart(ThreadProc));
                handle.IsBackground = false;
                handle.Name = _name;
                handle.Start();
                return true;
            }
            catch (Exception)
            {
                handle = null;
                return false;
            }
        }

        private void ThreadProc()
        {
            pthread.running = true;
            pthread.completed = false;
            try
            {
                threadMain();
            }
            finally
            {
                pthread.running = false;
                pthread.completed = true;
            }
        }

        /** Terminates the thread without notifying or
            waiting for a cancelation point. */
        public void terminate()
        {
            if (handle != null)
            {
                handle.Abort();
                handle = null;
            }
        }

        /**
            Returns true if threadMain is currently executing. */
        public bool running()
        {
            return pthread.running;
        }

        /** Returns completed status of thread. */
        public bool completed()
        {
            return pthread.completed;
        }

        /** 
            Waits for the thread to finish executing. 
            
            TODO: Does this need a timeout? */
        public void waitForCompletion()
        {
            Debug.debugAssert(handle != null);
            if (handle != null)
            {
                handle.Join();
            }
        }

        /** Returns thread name */
        public string name()
        {
            return _name;
        }

        protected abstract void threadMain();

        private sealed class BasicThread : GThread
        {
            private readonly Action wrapperProc;

            public BasicThread(string name, Action proc)
                : base(name)
            {
                wrapperProc = proc;
            }

            protected override void threadMain()
            {
                wrapperProc();
            }
        }
    }

    /**
        GMutex's documentation
    */
    public sealed class GMutex : IDisposable
    {
        private readonly object handle;

        public GMutex()
        {
            handle = new object();
        }

        public void Dispose()
        {
        }

        /** Locks the mutex or blocks until available. */
        public void @lock()
        {
            Monitor.Enter(handle);
        }

        /** Locks the mutex or blocks until available. */
        public void lock_()
        {
            @lock();
        }

        /** Unlocks the mutex. */
        public void unlock()
        {
            Monitor.Exit(handle);
        }
    }

    /**
        GMutexLock's documentation.
        Automatically locks while in scope.
    */
    public sealed class GMutexLock : IDisposable
    {
        private readonly GMutex m;

        public GMutexLock(GMutex mutex)
        {
            m = mutex;
            m.lock_();
        }

        public void Dispose()
        {
            m.unlock();
        }
    }
}
