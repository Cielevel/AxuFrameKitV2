namespace AxuFrameKit._Core.DesignPattern
{
    /// <summary>
    /// 责任链模式中的处理者接口
    /// </summary>
    /// <typeparam name="TRequest">需要处理的请求类型</typeparam>
    public interface IHandler<TRequest>
    {
        IHandler<TRequest> SetNext(IHandler<TRequest> handler);
        void Handle(TRequest request);
    }

    /// <summary>
    /// 处理者的抽象基类
    /// </summary>
    public abstract class BaseHandler<TRequest> : IHandler<TRequest>
    {
        protected IHandler<TRequest> _nextHandler;

        public IHandler<TRequest> SetNext(IHandler<TRequest> handler)
        {
            _nextHandler = handler;
            // 返回传入的处理者以便进行链式调用，例如：handlerA.SetNext(handlerB).SetNext(handlerC)
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
