using System;

namespace KKStudioSocket.Commands
{
    public abstract class BaseCommandHandler
    {
        protected Action<string> SendCallback { get; set; }
        
        public BaseCommandHandler(Action<string> sendCallback)
        {
            SendCallback = sendCallback;
        }
        
        protected void Send(string data)
        {
            SendCallback?.Invoke(data);
        }
        
        protected void EnqueueAction(System.Action action)
        {
            KKStudioSocketPlugin.EnqueueMainThreadAction(action);
        }
    }
}