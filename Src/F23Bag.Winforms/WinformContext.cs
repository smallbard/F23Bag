using F23Bag.AutomaticUI;
using F23Bag.AutomaticUI.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Winforms
{
    public class WinformContext
    {
        public WinformContext(WinformsUIBuilder uiBuilder, I18n i18n, Func<Type, IAuthorization> getAuthorization, Func<Type, object> resolve)
        {
            UIBuilder = uiBuilder;
            I18n = i18n;
            GetAuthorization = getAuthorization;
            Resolve = resolve;
            SelectorOwnerProperties = new Dictionary<Layout, PropertyInfo>();
        }

        public WinformsUIBuilder UIBuilder { get; private set; }

        public I18n I18n { get; private set; }

        public Func<Type, IAuthorization> GetAuthorization { get; private set; }

        public Func<Type, object> Resolve { get; private set; }

        /// <summary>
        /// Get the reals properties in case of selector.
        /// </summary>
        public Dictionary<Layout, PropertyInfo> SelectorOwnerProperties { get; private set; }
    }
}
