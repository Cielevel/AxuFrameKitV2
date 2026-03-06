namespace AxuFrameKit._Core.DesignPattern
{
    /// <summary>
    /// 责任链模式中的处理者接口，定义了请求的处理和链节点的设置
    /// </summary>
    /// <typeparam name="TRequest">需要处理的请求类型</typeparam>
    public interface IHandler<TRequest>
    {
        /// <summary>
        /// 设置链中的下一个处理者
        /// </summary>
        /// <param name="handler">下一个处理者实例</param>
        /// <returns>返回传入的处理者以便进行链式调用</returns>
        IHandler<TRequest> SetNext(IHandler<TRequest> handler);

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="request">具体的请求数据</param>
        void Handle(TRequest request);
    }

    /// <summary>
    /// 处理者的抽象基类，实现了基本的链式跳转逻辑
    /// </summary>
    /// <typeparam name="TRequest">请求参数类型</typeparam>
    public abstract class BaseHandler<TRequest> : IHandler<TRequest>
    {
        /// <summary>
        /// 链中的下一跳处理者
        /// </summary>
        protected IHandler<TRequest> _nextHandler;

        /// <summary>
        /// 建立责任链连接
        /// </summary>
        /// <param name="handler">后续节点</param>
        /// <returns>返回后续节点</returns>
        public IHandler<TRequest> SetNext(IHandler<TRequest> handler)
        {
            _nextHandler = handler;
            // 返回传入的处理者以便进行链式调用，例如：handlerA.SetNext(handlerB).SetNext(handlerC)
            return handler;
        }

        /// <summary>
        /// 执行处理逻辑，若当前节点无法处理或需要传递，则交给下一节点
        /// </summary>
        /// <param name="request">请求数据</param>
        public virtual void Handle(TRequest request)
        {
            if (_nextHandler != null)
            {
                _nextHandler.Handle(request);
            }
        }
    }
}
