using OpenBudget.Application.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.PlatformServices
{
    public interface ISettingsProvider
    {
        void SaveSettings(SettingsBase settings);

        T Get<T>() where T : SettingsBase;
    }
}
