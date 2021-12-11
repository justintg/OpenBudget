using Avalonia.Controls;
using Avalonia.Controls.Templates;
using OpenBudget.Application.ViewModels;
using OpenBudget.Application.ViewModels.BudgetEditor;
using OpenBudget.Avalonia.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Avalonia
{
    public class ViewLocator : IDataTemplate
    {
        private Dictionary<Type, Type> _mappings = new Dictionary<Type, Type>();
        public ViewLocator()
        {
            _mappings = new Dictionary<Type, Type>();
            Map<WelcomeViewModel, WelcomeView>();
            Map<CreateBudgetViewModel, CreateBudgetView>();
            Map<MainBudgetViewModel, MainBudgetView>();
            Map<BudgetEditorViewModel, BudgetEditorView>();
        }

        private void Map<TViewModel, TView>()
        {
            _mappings.Add(typeof(TViewModel), typeof(TView));
        }

        public IControl Build(object param)
        {
            if (_mappings.TryGetValue(param.GetType(), out var viewType))
            {
                return (IControl)Activator.CreateInstance(viewType);
            }

            return new TextBlock { Text = $"Unknown type: {param.GetType().FullName}" };
        }

        public bool Match(object data)
        {
            if (data == null) return false;
            return _mappings.ContainsKey(data.GetType());
        }
    }
}
