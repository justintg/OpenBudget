using GalaSoft.MvvmLight;
using System;

namespace OpenBudget.Application.ViewModels
{
    public class UtilViewModelBase : ViewModelBase
    {
        protected void MountSet<T>(ref T obj, T value, Action<T> mount = null, Action<T> unmount = null)
        {
            if (obj != null && unmount != null)
            {
                unmount(obj);
            }
            obj = value;
            if (obj != null && mount != null)
            {
                mount(obj);
            }
        }
    }
}
