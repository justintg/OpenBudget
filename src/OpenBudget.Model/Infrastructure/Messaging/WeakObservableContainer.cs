using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Messaging
{
    public class WeakObservableContainer
    {
        private HashSet<IDisposable> _subscriptions = new HashSet<IDisposable>();

        public void RegisterObservable<T, TEntity>(IObservable<T> observable, TEntity entity, Action<TEntity, T> onNextAction) where TEntity : class
        {
            WeakReference<TEntity> weakReference = new WeakReference<TEntity>(entity);

            var observer = new WeakObserver<T, TEntity>(weakReference, onNextAction);
            IDisposable subscription = observable.Subscribe(observer);
            _subscriptions.Add(subscription);

            observer.EntityDisposed += (sender, e) =>
            {
                _subscriptions.Remove(subscription);
                subscription.Dispose();
            };
        }
    }

    public class WeakObserver<T, TEntity> : IObserver<T> where TEntity : class
    {
        private readonly WeakReference<TEntity> _entityReference;
        private readonly Action<TEntity, T> _onNextAction;

        public WeakObserver(WeakReference<TEntity> entityReference, Action<TEntity, T> onNextAction)
        {
            _entityReference = entityReference ?? throw new ArgumentNullException(nameof(entityReference));
            _onNextAction = onNextAction ?? throw new ArgumentNullException(nameof(onNextAction));
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(T value)
        {
            TEntity entity;
            if (_entityReference.TryGetTarget(out entity))
            {
                _onNextAction(entity, value);
            }
            else
            {
                RaiseEntityDisposed();
            }
        }

        public event EventHandler EntityDisposed;

        private void RaiseEntityDisposed() => EntityDisposed?.Invoke(this, EventArgs.Empty);
    }
}
