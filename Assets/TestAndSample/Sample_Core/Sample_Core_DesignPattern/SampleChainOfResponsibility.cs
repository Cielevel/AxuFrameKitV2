using AxuFrameKit._Core.DesignPattern;
using UnityEngine;

namespace TestAndSample.Sample_Core.Sample_Core_DesignPattern
{
    public class SampleChainOfResponsibility : MonoBehaviour
    {
        private IHandler<string> _handlerChain;

        private void Start()
        {
            // Build the chain: Info -> Warning -> Error
            var infoHandler    = new InfoHandler();
            var warningHandler = new WarningHandler();
            var errorHandler   = new ErrorHandler();

            infoHandler.SetNext(warningHandler).SetNext(errorHandler);
            _handlerChain = infoHandler;

            Debug.Log("Sample_ChainOfResponsibility Started.");
            Debug.Log("Press Q: Send 'INFO: Game Started'");
            Debug.Log("Press W: Send 'WARNING: Low Disk Space'");
            Debug.Log("Press E: Send 'ERROR: Connection Lost'");
            Debug.Log("Press R: Send 'DEBUG: Unknown message'");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _handlerChain.Handle("INFO: Game Started");
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                _handlerChain.Handle("WARNING: Low Disk Space");
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                _handlerChain.Handle("ERROR: Connection Lost");
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                _handlerChain.Handle("DEBUG: Unknown message");
            }
        }

        // Concrete Handlers

        private class InfoHandler : BaseHandler<string>
        {
            public override void Handle(string request)
            {
                if (request.StartsWith("INFO"))
                {
                    Debug.Log($"[InfoHandler] Handled: {request}");
                }
                else
                {
                    base.Handle(request);
                }
            }
        }

        private class WarningHandler : BaseHandler<string>
        {
            public override void Handle(string request)
            {
                if (request.StartsWith("WARNING"))
                {
                    Debug.LogWarning($"[WarningHandler] Handled: {request}");
                }
                else
                {
                    base.Handle(request);
                }
            }
        }

        private class ErrorHandler : BaseHandler<string>
        {
            public override void Handle(string request)
            {
                if (request.StartsWith("ERROR"))
                {
                    Debug.LogError($"[ErrorHandler] Handled: {request}");
                }
                else
                {
                    base.Handle(request);
                }
            }
        }
    }
}
