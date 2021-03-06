namespace Fibrous
{
    using System;

    /// <summary>
    /// Channel that maintains its last value which is passed to new subscribers
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class StateChannel<T> : IChannel<T>
    {
        private readonly object _lock = new object();
        private T _last;
        private bool _hasValue;
        private readonly IChannel<T> _updateChannel = new Channel<T>();

        public IDisposable Subscribe(IFiber fiber, Action<T> handler)
        {
            lock (_lock)
            {
                IDisposable disposable = _updateChannel.Subscribe(fiber, handler);
                if (_hasValue)
                {
                    T item = _last;
                    fiber.Enqueue(() => handler(item));
                }
                return disposable;
            }
        }

        public bool HasValue
        {
            get
            {
                lock (_lock)
                {
                    return _hasValue;
                }
            }
        }
        public T Current
        {
            get
            {
                lock (_lock)
                {
                    return _last;
                }
            }
        }

        public bool Publish(T msg)
        {
            lock (_lock)
            {
                _last = msg;
                _hasValue = true;
                return _updateChannel.Publish(msg);
            }
        }
    }
}