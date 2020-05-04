using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenBudget.Model.Infrastructure.Messaging
{
    public interface IMessenger<T>
    {
        void RegisterForMessages<TMessage>(string entityName, IHandler<TMessage> handler) where TMessage : T;
        void PublishEvent<TMessage>(string entityName, TMessage message) where TMessage : T;
    }

    public class Messenger<T> : IMessenger<T>
    {
        private readonly Dictionary<Tuple<string, Type>, object> _registrations = new Dictionary<Tuple<string, Type>, object>();

        public virtual void PublishEvent<TMessage>(string entityName, TMessage message) where TMessage : T
        {
            if (message.GetType() != typeof(TMessage))
            {
                var publishMethod = this.GetType().GetMethod("PublishEvent");
                var method = publishMethod.MakeGenericMethod(message.GetType());
                method.Invoke(this, new object[] { entityName, message });
            }
            else
            {
                foreach (var handler in GetHandlers<TMessage>(entityName))
                {
                    handler.Handle((message));
                }
            }
        }

        public virtual void RegisterForMessages<TMessage>(string entityName, IHandler<TMessage> handler) where TMessage : T
        {
            List<WeakReference<IHandler<TMessage>>> registrationList = GetOrCreateHandlerList<TMessage>(entityName);
            WeakReference<IHandler<TMessage>> handlerReference = new WeakReference<IHandler<TMessage>>(handler);
            registrationList.Add(handlerReference);
        }

        protected IEnumerable<IHandler<TMessage>> GetHandlers<TMessage>(string entityName) where TMessage : T
        {
            var handlerList = GetHandlerList<TMessage>(entityName);
            if (handlerList == null) yield break;
            foreach (var handlerReference in handlerList.ToList())
            {
                IHandler<TMessage> handler;
                if (handlerReference.TryGetTarget(out handler))
                {
                    yield return handler;
                }
                else
                {
                    handlerList.Remove(handlerReference);
                }
            }
        }
        protected List<WeakReference<IHandler<TMessage>>> GetOrCreateHandlerList<TMessage>(string entityName) where TMessage : T
        {
            List<WeakReference<IHandler<TMessage>>> registrationList;
            object listAsObject;

            Tuple<string, Type> key = new Tuple<string, Type>(entityName, typeof(TMessage));

            if (!_registrations.TryGetValue(key, out listAsObject))
            {
                registrationList = new List<WeakReference<IHandler<TMessage>>>();
                _registrations[key] = registrationList;
            }
            else
            {
                registrationList = (List<WeakReference<IHandler<TMessage>>>)listAsObject;
            }
            return registrationList;
        }

        protected List<WeakReference<IHandler<TMessage>>> GetHandlerList<TMessage>(string entityName) where TMessage : T
        {
            object listAsObject;

            Tuple<string, Type> key = new Tuple<string, Type>(entityName, typeof(TMessage));

            if (_registrations.TryGetValue(key, out listAsObject))
            {
                var registrationList = (List<WeakReference<IHandler<TMessage>>>)listAsObject;
                return registrationList;
            }
            return null;
        }
    }
}
