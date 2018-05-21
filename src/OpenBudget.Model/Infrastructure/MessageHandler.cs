using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure
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
