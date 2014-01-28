using GalaSoft.MvvmLight;
using K4W2Accuracy.ViewModel;
using GistModulesLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GistModulesLib
{
    [InheritedExport(ViewModelTypes.NonVisualModule, typeof(NonVisualModule))]
    public abstract class NonVisualModule : ModuleBase
    {
        /// <summary>
        /// Constructor for the NonVisualModule class
        /// </summary>
        /// <p name="title">The title of the module</p>
        public NonVisualModule(string title = "Default Title", bool isActive = true) : base(isActive)
        {
            Title = title;
        }
    }
}
