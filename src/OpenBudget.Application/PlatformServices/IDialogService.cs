using OpenBudget.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.PlatformServices
{
    public interface IDialogService
    {
        /// <summary>
        /// Shows a Window for a certain ViewModel
        /// </summary>
        /// <typeparam name="T">The type of the ViewModel, it must subclass <see cref="ClosableViewModel"/></typeparam>
        /// <param name="viewModel">The ViewModel to open a window for.</param>
        /// <returns>The Window that was created.</returns>
        object ShowWindow<T>(T viewModel) where T : ClosableViewModel;

        /// <summary>
        /// Show's a message to the user.
        /// </summary>
        /// <param name="message">The message to show.</param>
        void ShowMessage(string message);

        /// <summary>
        /// Show's an error to the user.
        /// </summary>
        /// <param name="message">The message to show.</param>
        void ShowError(string message);
    }
}
