using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GazeStream.ViewModels
{
    public static class ViewModelsLocator
    {
        public static CursorSettingsViewModel CursorSettingsViewModel = new();
        public static InteractionSettingsViewModel InteractionSettingsViewModel = new();

    }
}
