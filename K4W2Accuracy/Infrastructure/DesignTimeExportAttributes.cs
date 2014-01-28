using System;
using System.ComponentModel;
using System.ComponentModel.Composition;

namespace K4W2Accuracy.Infrastructure
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    /// <summary>;
    /// Marks an export as a design-time replacement for another export with the same contract.
    /// </summary>
    public class DesignTimeExportAttribute : ExportAttribute
    {
        #region Constructors
        public DesignTimeExportAttribute()
        {
            DesignTime = false;
        }

        public DesignTimeExportAttribute(Type contractType)
            : base(contractType)
        {
            DesignTime = false;
        }

        public DesignTimeExportAttribute(string contractName)
            : base(contractName)
        {
            DesignTime = false;
        }

        public DesignTimeExportAttribute(string contractName, Type contractType)
            : base(contractName, contractType)
        {
            DesignTime = false;
        }
        #endregion

        [DefaultValue(false)]
        public bool DesignTime
        {
            get;
            set;
        }
    }
}
