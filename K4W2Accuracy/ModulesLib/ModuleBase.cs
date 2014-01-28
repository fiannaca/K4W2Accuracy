using GalaSoft.MvvmLight;
using K4W2Accuracy;
using K4W2Accuracy.Model;
using K4W2Accuracy.ViewModel;
using System.ComponentModel.Composition;

namespace GistModulesLib
{
    public abstract class ModuleBase : ViewModelBase
    {
        public ModuleBase(bool isActive)
        {
            _isActive = isActive;
        }

        /// <summary>
        /// A reference to te Kinect Helper object
        /// </summary>
        [Import(typeof(IKinectHelper))]
        public KinectHelper Helper;

        /// <summary>
        /// Gets a reference to the ViewModelLocator
        /// </summary>
        public ViewModelLocator Locator
        {
            get
            {
                return (ViewModelLocator)App.Current.Resources["Locator"];
            }
        }

        /// <summary>
        /// The next ID value to assign to a module
        /// </summary>
        static int nextId = 0;

        /// <summary>
        /// Gets the next id and increments the NextID value
        /// </summary>
        /// <returns>Returns the module ID</returns>
        static private int _id()
        {
            int id = nextId;
            ++nextId;

            return id;
        }

        /// <summary>
        /// A unique integer identifier for this module
        /// </summary>
        public readonly int SubscriberID = _id();

        /// <summary>
        /// The <see cref="Title" /> property's name.
        /// </summary>
        public const string TitlePropertyName = "Title";

        private string _title = "Default Title";

        /// <summary>
        /// Sets and gets the Title property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Title
        {
            get
            {
                return _title;
            }

            set
            {
                if (_title == value)
                {
                    return;
                }

                RaisePropertyChanging(TitlePropertyName);
                _title = value;
                RaisePropertyChanged(TitlePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="IsActive" /> property's name.
        /// </summary>
        public const string IsActivePropertyName = "IsActive";

        private bool _isActive = true;

        /// <summary>
        /// Sets and gets the IsActive property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public virtual bool IsActive
        {
            get
            {
                return _isActive;
            }

            set
            {
                if (_isActive == value)
                {
                    return;
                }

                RaisePropertyChanging(IsActivePropertyName);

                if(value)
                {
                    if(Helper != null)
                    {
                        Helper.Continue(this.SubscriberID);
                    }
                }
                else
                {
                    if(Helper != null)
                    {
                        Helper.Pause(this.SubscriberID);
                    }
                }

                _isActive = value;
                RaisePropertyChanged(IsActivePropertyName);
            }
        }
    }
}
