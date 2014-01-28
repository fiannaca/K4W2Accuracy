using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using K4W2Accuracy.Infrastructure;
using GistModulesLib;
using Microsoft.Practices.ServiceLocation;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;

namespace K4W2Accuracy.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings. This class
    /// extends the ViewModelBase class only so that the IsInDesignTime property 
    /// can be accessed.
    /// </summary>
    public class ViewModelLocator : ViewModelBase
    {
        public static CompositionContainer Container;

        private readonly string ExtensionsDir;

        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            //detect design-time
            //TODO: find a better method to do this than extending ViewModelBase
            bool designTime = ViewModelBase.IsInDesignModeStatic;

            ExtensionsDir = Directory.GetCurrentDirectory() + "/Extensions";

            //since the default catalog initializing in MEF doesn't work at runtime
            //we have to create the catalog manually for now
            var aggregatedAssemblyCatalog = new AggregateCatalog(
                                                    new AssemblyCatalog(typeof(MainViewModel).Assembly), //assembly that contains the ViewModels
                                                    new DirectoryCatalog(ExtensionsDir)
                                            );

            //the design/run time catalog helps to filter exports depending on a designtime attribuete
            var drpCatalog = new DRPartCatalog(aggregatedAssemblyCatalog, designTime);

            //the container to resolve exported ViewModels
            Container = new CompositionContainer(drpCatalog);

            Messenger.Default.Register<KinectStatusMessage>(this, (msg) =>
            {
                if (msg.State == KinectState.Shutdown)
                {
                    Cleanup();
                }
            });
        }

        /// <summary>
        /// String indexer for accessing View Models by contract name
        /// </summary>
        /// <p name="viewModel">Contract name of a view Model</p>
        /// <returns>The view Model reference</returns>
        public object this[string viewModel]
        {
            get
            {
                return Container.GetExportedValue<object>(viewModel);
            }
        }

        /// <summary>
        /// Typed property for accessing the MainViewModel.
        /// </summary>
        public MainViewModel MainViewModel 
        { 
            get 
            { 
                return (MainViewModel)this[ViewModelTypes.MainViewModel]; 
            } 
        }
        
        public static void Cleanup()
        {
            //Clean up the MainViewModel
            var exMainViewModel = Container.GetExport<object>(ViewModelTypes.MainViewModel);
            ((MainViewModel)exMainViewModel.Value).Cleanup();
            Container.ReleaseExport(exMainViewModel);

            //Cleanup all remaining visual modules
            var OtherVisualModules = Container.GetExports<object>(ViewModelTypes.VisualModule);
            foreach (Lazy<object> mod in OtherVisualModules)
            {
                ((VisualModule)mod.Value).Cleanup();
            }

            //Cleanup all remaining non-visual modules
            var OtherNonvisualModules = Container.GetExports<object>(ViewModelTypes.NonVisualModule);
            foreach (Lazy<object> mod in OtherNonvisualModules)
            {
                ((NonVisualModule)mod.Value).Cleanup();
            }
        }
    }
}