namespace AxuFrameKit._Core.DesignPattern
{
    /// <summary>
    /// Interface for a handler in the Chain of Responsibility
    /// </summary>
    /// <typeparam name="TRequest">The type of request to handle</typeparam>
    public interface IHandler<TRequest>
    {
        IHandler<TRequest> SetNext(IHandler<TRequest> handler);
        void Handle(TRequest request);
    }

    /// <summary>
    /// Abstract base class for handlers
    /// </summary>
    public abstract class BaseHandler<TRequest> : IHandler<TRequest>
    {
        protected IHandler<TRequest> _nextHandler;

        public IHandler<TRequest> SetNext(IHandler<TRequest> handler)
        {
            _nextHandler = handler;
            // Return the passed handler so we can link like: handlerA.SetNext(handlerB).SetNext(handlerC)
            return handler;
        }

        public virtual void Handle(TRequest request)
        {
            if (_nextHandler != null)
            {
                _nextHandler.Handle(request);
            }
        }
    }
}
