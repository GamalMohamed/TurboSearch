using System.Collections.Generic;
using System.Threading;

namespace TurboSearch
{
    public class UrlInfo
    {
        private readonly string _url;

        public UrlInfo(string urlString)
        {
            this._url = urlString;
        }
        //string
        public int Depth { get; set; }

        public string UrlString
        {
            get
            {
                return this._url;
            }
        }

    }

    public abstract class SecurityQueue<T>
        where T : class
    {
        protected readonly Queue<T> InnerQueue = new Queue<T>();

        protected readonly object SyncObject = new object();

        private readonly AutoResetEvent autoResetEvent;


        protected SecurityQueue()
        {
            this.autoResetEvent = new AutoResetEvent(false);
        }

        public delegate bool BeforeEnQueueEventHandler(T target);

        public event BeforeEnQueueEventHandler BeforeEnQueueEvent;

        public AutoResetEvent AutoResetEvent
        {
            get
            {
                return this.autoResetEvent;
            }
        }

        public int Count
        {
            get
            {
                lock (this.SyncObject)
                {
                    return this.InnerQueue.Count;
                }
            }
        }

        public bool HasValue
        {
            get
            {
                return this.Count != 0;
            }
        }

        public T DeQueue()
        {
            lock (this.SyncObject)
            {
                if (this.InnerQueue.Count > 0)
                {
                    return this.InnerQueue.Dequeue();
                }

                return default(T);
            }
        }

        public void EnQueue(T target)
        {
            lock (this.SyncObject)
            {
                if (this.BeforeEnQueueEvent != null)
                {
                    if (this.BeforeEnQueueEvent(target))
                    {
                        this.InnerQueue.Enqueue(target);
                    }
                }
                else
                {
                    this.InnerQueue.Enqueue(target);
                }

                this.AutoResetEvent.Set();
            }
        }

    }

    public class UrlQueue : SecurityQueue<UrlInfo>
    {
        private UrlQueue()
        {
        }

        public static UrlQueue Instance
        {
            get
            {
                return Nested.Inner;
            }
        }

        private static class Nested
        {
            internal static readonly UrlQueue Inner = new UrlQueue();
        }
    }

}
