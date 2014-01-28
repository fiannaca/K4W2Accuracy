using GalaSoft.MvvmLight;

namespace K4W2Accuracy.Infrastructure
{
    /// <summary>
    /// This class allows for displaying properties in the "System Config" and "System Info" 
    /// areas of the user interface. NOTE: This is a fast and loose implementation which 
    /// DOES NOT do any type checking! The value passed to the Value property can be anything
    /// which has the "object" base class (i.e. everything). This is clearly not an ideal 
    /// implementation of this feature, but it will work for the time being.
    /// </summary>
    public class VisualProperty : ObservableObject
    {
        /// <summary>
        /// Use this constant as the name of the export contract for exporting properties
        /// that should be displayed in the "Module Info" section of the interface
        /// </summary>
        public const string ModuleInfo = "ModuleInfo";

        /// <summary>
        /// Use this constant as the name of the export contract for exporting properties
        /// that should be displayed in the "Module Info" section of the interface which
        /// may have too much output for a single line.
        /// </summary>
        public const string ModuleInfoLarge = "ModuleInfoLarge";

        /// <summary>
        /// Use this constant as the name of the export contract for exporting properties
        /// that should be displayed in the "System Info" section of the interface
        /// </summary>
        public const string SystemInfo = "SystemInfo";

        /// <summary>
        /// Parameterized constructor ensures that users always provide the system with 
        /// a display label for the interface. Additionally, this constructor allows the 
        /// developer to determine the order in which visual properties are displayed in
        /// the interface. All properties default to a SortOrder value of zero. Larger
        /// numbers are displayed at the top.
        /// </summary>
        /// <param name="label">The label which is printed in the interface</param>
        /// <param name="order">The sort ordering weight for this property</param>
        public VisualProperty(string label, int order = 0)
        {
            Label = label;
            SortOrder = order;
        }

        /// <summary>
        /// Gets and sets the interface label for this property
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The <see cref="DisplayString" /> property's name.
        /// </summary>
        public const string PropertyDisplayName = "DisplayString";

        private object _visualProp = null;

        /// <summary>
        /// Gets the DisplayString property. 
        /// </summary>
        public string DisplayString
        {
            get
            {
                return _visualProp == null ? "" : _visualProp.ToString();
            }
        }

        /// <summary>
        /// Sets the VisualProperty's value. Changes to that property's value raise 
        /// the PropertyChanged event.
        /// </summary>
        public object Value
        {
            set
            {
                if (_visualProp != null && _visualProp.Equals(value))
                {
                    return;
                }

                RaisePropertyChanging(PropertyDisplayName);
                _visualProp = value;
                RaisePropertyChanged(PropertyDisplayName);
            }
        }

        /// <summary>
        /// Set this value in order to control the order items are displayed on the user 
        /// interface. This works like a z-index. Larger numbers end up on top.
        /// </summary>
        public int SortOrder { get; set; }
    }
}
