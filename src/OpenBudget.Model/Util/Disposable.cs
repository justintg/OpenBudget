using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Util
{
    public static class Disposable
    {
        private class ActionDisposable : IDisposable
        {
            private readonly Action _disposeAction;

            public ActionDisposable(Action disposeAction)
            {
                _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
            }
            public void Dispose()
            {
                _disposeAction();
            }
        }

        private class MultiDisposable : IDisposable
        {
            private readonly IDisposable[] _disposables;

            public MultiDisposable(IDisposable[] disposables)
            {
                _disposables = disposables ?? throw new ArgumentNullException(nameof(disposables));
            }

            public void Dispose()
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }
            }
        }

        public static IDisposable Create(Action disposeAction)
        {
            return new ActionDisposable(disposeAction);
        }

        public static IDisposable Create(params IDisposable[] disposables)
        {
            return new MultiDisposable(disposables);
        }
    }
}
