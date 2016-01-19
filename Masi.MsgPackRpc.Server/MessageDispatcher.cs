using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Masi.MsgPackRpc.Common;

namespace Masi.MsgPackRpc.Server
{
    public interface IMessageDispatcher
    {
        void DispatchMessage(RpcMessage message);
    }

    public abstract class MessageDispatcher : IMessageDispatcher
    {
        private readonly int _maxThreads;
        private readonly ManualResetEventSlim _queueEvent = new ManualResetEventSlim(false);
        private readonly ConcurrentQueue<RpcMessage> _queue = new ConcurrentQueue<RpcMessage>();

        private int _threadCount = 0;
        private int _activeThreads = 0;

        public MessageDispatcher(int maxThreads)
        {
            _maxThreads = maxThreads;
        }

        public void DispatchMessage(RpcMessage message)
        {
            _queue.Enqueue(message);
            _queueEvent.Set();

            CheckStartThread();
        }

        private void RunProcessMessages()
        {
            RpcMessage message = null;

            while (true)
            {
                if (!_queueEvent.Wait(10000))
                    break;

                while (_queue.TryDequeue(out message))
                {
                    DoProcessMessage(message);
                }

                _queueEvent.Reset();

                while (_queue.TryDequeue(out message))
                {
                    _queueEvent.Set();
                    DoProcessMessage(message);
                }
            }

            Interlocked.Decrement(ref _threadCount);

            if (_queue.TryDequeue(out message))
            {
                _queueEvent.Set();

                Interlocked.Increment(ref _threadCount);
                DoProcessMessage(message);
                RunProcessMessages();
            }
        }

        private void DoProcessMessage(RpcMessage message)
        {
            try
            {
                Interlocked.Increment(ref _activeThreads);
                ProcessMessage(message);
                Interlocked.Decrement(ref _activeThreads);
            }
            catch (Exception e)
            {
                OnMessageException(message, e);
            }
        }

        public abstract void ProcessMessage(RpcMessage message);
        public abstract void OnMessageException(RpcMessage message, Exception exception);

        private void CheckStartThread()
        {
            SpinWait? spin = null;

            int curThreadCount;
            int newThreadCount;
            while (true)
            {
                curThreadCount = Volatile.Read(ref _threadCount);

                if (curThreadCount >= _maxThreads)
                    return; // Max number of threads reached

                int activeThreads = Volatile.Read(ref _activeThreads);
                if (curThreadCount > activeThreads)
                    return; // Inactive threads available

                newThreadCount = curThreadCount + 1;

                if (Interlocked.CompareExchange(ref _threadCount, newThreadCount, curThreadCount) == curThreadCount)
                {
                    // Spawn new thread
                    new Thread(RunProcessMessages) { IsBackground = true }.Start();
                    return;
                }

                if (!spin.HasValue)
                    spin = new SpinWait();
                spin.Value.SpinOnce();
            }
        }
    }
}
