using System;

namespace OpenBudget.Model.Infrastructure.Messaging
{
    public class MessageHandler<T> : IHandler<T>
    {
        Action<T> _handleAction;

        public MessageHandler(Action<T> handleAction)
        {
            _handleAction = handleAction;
        }

        public void Handle(T message)
        {
            _handleAction(message);
        }
    }
}
