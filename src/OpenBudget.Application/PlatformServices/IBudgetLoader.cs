using OpenBudget.Application.Model;
using OpenBudget.Model;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.PlatformServices
{
    /// <summary>
    /// A per-platform service that knows how to construct a BudgetModel. This interface is necessary because a platform
    /// may have its own implementation of <see cref="IEventStore"/> or <see cref="ISynchronizationService"/>. Additionally
    /// a platform may provide more or less control about where a budget can be saved.
    /// </summary>
    public interface IBudgetLoader
    {
        /// <summary>
        /// Creates a BudgetModel at the given path with a default Budget entity
        /// </summary>
        /// <param name="budgetPath">The path where the budget should be created.</param>
        /// <returns>The BudgetModel</returns>
        BudgetModel CreateNewBudget(string budgetPath);

        /// <summary>
        /// Creates a BudgetModel at the given path with a given Budget entity attached.
        /// </summary>
        /// <param name="budgetPath">The path where the budget should be created.</param>
        /// <param name="initialBudget">The initial Budget to be attached.</param>
        /// <returns>The BudgetModel</returns>
        BudgetModel CreateNewBudget(string budgetPath, Budget initialBudget);

        /// <summary>
        /// Loads a Budget from a given path.
        /// </summary>
        /// <param name="budgetPath">The path to load the Budget from</param>
        /// <returns>The BudgetModel</returns>
        BudgetModel LoadBudget(string budgetPath);

        /// <summary>
        /// Asks the platform to verify that a Budget exists, is valid, and can be opened at
        /// a given path.
        /// </summary>
        /// <param name="budgetPath">The budget path to be checked.</param>
        /// <returns>A <see cref="ValidBudgetCheck"/> that contains information about the budget
        /// such as if it is valid or not, the Budget's name, and a list of errors</returns>
        ValidBudgetCheck IsBudgetValid(string budgetPath);

        /// <summary>
        /// Asks the platform to ask the user for a save path with an optional
        /// default name.
        /// </summary>
        /// <param name="defaultName"></param>
        /// <returns>A path that can be used with CreateNewBudget</returns>
        string GetBudgetSavePath(string defaultName = null);

        /// <summary>
        /// Ask the platform to ask the user which Budget to open.
        /// </summary>
        /// <returns>The path to the budget that can be used with LoadBudget.</returns>
        string GetBudgetOpenPath();
    }
}
