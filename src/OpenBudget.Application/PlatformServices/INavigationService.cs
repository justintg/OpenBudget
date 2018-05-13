using OpenBudget.Application.ViewModels;

namespace OpenBudget.Application.PlatformServices
{
    /// <summary>
    /// Interface for working with windows in a non-platform specific way.
    /// </summary>
    public interface INavigationService
    {
        void NavigateTo<T>(bool storeBack = false) where T : ClosableViewModel;

        bool CanGoBack();

        void NavigateBack();
    }
}