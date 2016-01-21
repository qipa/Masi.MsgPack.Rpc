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
    public interface IRequestDispatcher
    {
        void DispatchRequest(IChannelRequest request);
    }

    public abstract class RequestDispatcher : IRequestDispatcher
    {
        private readonly int _maxThreads;
        private readonly ManualResetEventSlim _queueEvent = new ManualResetEventSlim(false);
        private readonly ConcurrentQueue<IChannelRequest> _queue = new ConcurrentQueue<IChannelRequest>();

        private int _threadCount = 0;
        private int _activeThreads = 0;

        public RequestDispatcher(int maxThreads)
        {
            _maxThreads = maxThreads;
        }

        public void DispatchRequest(IChannelRequest request)
        {
            _queue.Enqueue(request);
            _queueEvent.Set();

            CheckStartThread();
        }

        private void RunProcessRequests()
        {
            IChannelRequest request = null;

            while (true)
            {
                if (!_queueEvent.Wait(30000))
                    break;

                while (_queue.TryDequeue(out request))
                {
                    DoProcessRequests(request);
                }

                _queueEvent.Reset();

                // Avoids possible race condition with concurrent Enqueue->Set->Reset
                while (_queue.TryDequeue(out request))
                {
                    _queueEvent.Set();
                    DoProcessRequests(request);
                }
            }

            Interlocked.Decrement(ref _threadCount);

            // Avoids possible race condition with concurrent LoopBreak->Enqueue->Set
            if (_queue.TryDequeue(out request))
            {
                _queueEvent.Set();

                Interlocked.Increment(ref _threadCount);
                DoProcessRequests(request);
                RunProcessRequests();
            }
        }

        private void DoProcessRequests(IChannelRequest request)
        {
            try
            {
                Interlocked.Increment(ref _activeThreads);
                ProcessRequest(request);
                Interlocked.Decrement(ref _activeThreads);
            }
            catch (Exception e)
            {
                OnRequestException(request, e);
            }
        }

        public abstract void ProcessRequest(IChannelRequest request);
        public abstract void OnRequestException(IChannelRequest request, Exception exception);

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
                    new Thread(RunProcessRequests) { IsBackground = true }.Start();
                    return;
                }

                if (!spin.HasValue)
                    spin = new SpinWait();
                spin.Value.SpinOnce();
            }
        }
    }
}
