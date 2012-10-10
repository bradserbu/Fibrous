namespace Fibrous.Fibers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public abstract class GuiFiberBase : FiberBase
    {
        private readonly object _lock = new object();
        private readonly IExecutionContext _executionContext;
        private readonly IExecutor _executor;
        private readonly List<Action> _queue = new List<Action>();
        private volatile ExecutionState _started = ExecutionState.Created;

        protected GuiFiberBase(IExecutionContext executionContext, IExecutor executor)
        {
            _executionContext = executionContext;
            _executor = executor;
        }

        public override void Enqueue(Action action)
        {
            if (_started == ExecutionState.Stopped)
                return;
            if (_started == ExecutionState.Created)
            {
                lock (_lock)
                {
                    if (_started == ExecutionState.Created)
                    {
                        _queue.Add(action);
                        return;
                    }
                }
            }
            _executionContext.Enqueue(() => _executor.Execute(action));
        }

        public override void Start()
        {
            if (_started == ExecutionState.Running)
                throw new ThreadStateException("Already Started");
            lock (_lock)
            {
                List<Action> actions = _queue.ToList();
                _queue.Clear();
                if (actions.Count > 0)
                    _executionContext.Enqueue(() => _executor.Execute(actions));
                _started = ExecutionState.Running;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _started = ExecutionState.Stopped;
            base.Dispose(disposing);
        }
    }
}