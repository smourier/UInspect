using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace UInspect.Utilities
{
    public sealed class SingleThreadTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly AutoResetEvent _stop = new AutoResetEvent(false);
        private readonly AutoResetEvent _dequeue = new AutoResetEvent(false);
        private readonly ConcurrentQueue<Task> _tasks = new ConcurrentQueue<Task>();
        private readonly Thread _thread;

        public event EventHandler Executing;

        public SingleThreadTaskScheduler()
            : this(null)
        {
        }

        public SingleThreadTaskScheduler(Func<Thread, bool> threadConfigure = null)
        {
            DisposeThreadJoinTimeout = 1000;
            WaitTimeout = 1000;

            _thread = new Thread(SafeThreadExecute);
            _thread.IsBackground = true;

            if (threadConfigure != null)
            {
                if (!threadConfigure(_thread))
                    return;
            }

            if (_thread.Name == null)
            {
                _thread.Name = string.Format("_stts{0}", GetHashCode());
            }

            Extensions.Log("Scheduler thread id: " + _thread.ManagedThreadId);
            _thread.Start();
        }

        public bool IsRunningAsThread => _thread?.ManagedThreadId == Thread.CurrentThread.ManagedThreadId;
        public DateTime LastDequeueTime { get; private set; }
        public bool DequeueOnDispose { get; set; }
        public int DisposeThreadJoinTimeout { get; set; }
        public int WaitTimeout { get; set; }
        public int DequeueTimeout { get; set; }
        public int QueueCount => _tasks.Count;

        public void ClearQueue() => Dequeue(false);
        public bool TriggerDequeue()
        {
            if (DequeueTimeout <= 0)
                return _dequeue != null && _dequeue.Set();

            var ts = DateTime.Now - LastDequeueTime;
            if (ts.TotalMilliseconds < DequeueTimeout)
                return false;

            LastDequeueTime = DateTime.Now;
            return _dequeue != null && _dequeue.Set();
        }

        public void Dispose()
        {
            _stop.Set();
            _stop.Dispose();

            _dequeue.Dispose();

            if (DequeueOnDispose)
            {
                Dequeue(true);
            }

            if (_thread != null && _thread.IsAlive)
            {
                _thread.Join(DisposeThreadJoinTimeout);
            }
        }

        private int Dequeue(bool execute)
        {
            int count = 0;
            do
            {
                if (!_tasks.TryDequeue(out var task))
                    break;

                if (execute)
                {
                    Executing?.Invoke(this, EventArgs.Empty);
                    TryExecuteTask(task);
                }
                count++;
            }
            while (true);
            return count;
        }

        private void SafeThreadExecute()
        {
            if (Debugger.IsAttached)
            {
                ThreadExecute();
                return;
            }

            try
            {
                ThreadExecute();
            }
            catch
            {
                //+ continue
            }
        }

        private void ThreadExecute()
        {
            do
            {
                if (_stop == null || _dequeue == null)
                    return;

                _ = Dequeue(true);

                var i = WaitHandle.WaitAny(new[] { _stop, _dequeue }, WaitTimeout);
                if (i == 0)
                    break;

                _ = Dequeue(true);
            }
            while (true);
        }

        protected override void QueueTask(Task task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            _tasks.Enqueue(task);
            TriggerDequeue();
        }

        protected override IEnumerable<Task> GetScheduledTasks() => _tasks;
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;

        public void RunAutomationTask(Action action, bool startNew = false) => RunAutomationTaskAsync(action, startNew).Wait();
        public Task RunAutomationTaskAsync(Action action, bool startNew = false)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (!startNew && IsRunningAsThread)
            {
                action();
                return Task.CompletedTask;
            }

            return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, this);
        }

        public T RunAutomationTask<T>(Func<T> func, bool startNew = false) => RunAutomationTaskAsync(func, startNew).Result;
        public Task<T> RunAutomationTaskAsync<T>(Func<T> func, bool startNew = false)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            if (!startNew && IsRunningAsThread)
                return Task.FromResult(func());

            return Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, this);
        }
    }
}
