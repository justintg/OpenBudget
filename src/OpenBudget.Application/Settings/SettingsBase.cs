using OpenBudget.Application.PlatformServices;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace OpenBudget.Application.Settings
{
    [DataContract]
    public abstract class SettingsBase
    {
        protected SettingsBase(ISettingsProvider settingsProvider)
        {
            SetToDefault();
            _settingsProvider = settingsProvider;
        }

        [IgnoreDataMember]
        protected ISettingsProvider _settingsProvider;

        public abstract void SetToDefault();

        public void Save()
        {
            _settingsProvider.SaveSettings(this);
        }
    }
}
