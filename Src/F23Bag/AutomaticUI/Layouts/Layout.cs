using F23Bag.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace F23Bag.AutomaticUI.Layouts
{
    /// <summary>
    /// Base class for the layouts.
    /// </summary>
    public abstract class Layout
    {
        private readonly IEnumerable<ILayoutProvider> _layoutProviders;
        private readonly Dictionary<string, object> _options;

        protected Layout(IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string,object> options)
        {
            _layoutProviders = layoutProviders;
            _options = options;
        }

        public Type SelectorType { get; private set; }

        internal PropertyInfo SelectorOriginalProperty { get; private set; }
        
        protected bool IgnoreCloseBehavior { get; private set; }

        /// <summary>
        /// Return the DataGrid layout for a type.
        /// </summary>
        /// <param name="dataType">Type for which a DataGrid layout is needed.</param>
        /// <returns>DataGrid layout for the type.</returns>
        public DataGridLayout GetDataGridLayout(Type dataType)
        {
            return (DataGridLayout)Load(dataType, _layoutProviders, true, dataType, true, _options, true);
        }

        /// <summary>
        /// Return the layout for a type.
        /// </summary>
        /// <param name="dataType">Type for which a layout is needed.</param>
        /// <returns>Layout for the type.</returns>
        public Layout GetCreateUpdateLayout(Type dataType)
        {
            return Load(dataType, _layoutProviders, false, dataType, true, _options, false);
        }

        /// <summary>
        /// Return the layout for a property.
        /// </summary>
        /// <remarks>The returned layout can be a layout for a selector. SelectorType must be checked on the returned layout.</remarks>
        /// <param name="property">Property for which a layout is needed.</param>
        /// <param name="owner">Instance of the property's class.</param>
        /// <returns>Layout for the property.</returns>
        public Layout GetCreateUpdateLayout(PropertyInfo property, object owner)
        {
            var value = property.GetValue(owner);
            var layout = Load(property.PropertyType, _layoutProviders, true, value == null ? property.PropertyType : value.GetType(), false, _options, false);
            if (layout.SelectorType != null) layout.SelectorOriginalProperty = property;
            return layout;
        }

        /// <summary>
        /// Return the layout for a parameter.
        /// </summary>
        /// <remarks>The returned layout can be a layout for a selector. SelectorType must be checked on the returned layout.</remarks>
        /// <param name="property">Parameter for which a layout is needed.</param>
        /// <returns>Layout for the parameter.</returns>
        public Layout GetCreateUpdateLayout(ParameterInfo parameter)
        {
            return Load(parameter.ParameterType, _layoutProviders, true, parameter.ParameterType, false, _options, false);
        }

        internal static Layout Load(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, bool ignoreCloseBehavior, Type realDataType, bool ignoreSelector, Dictionary<string, object> options, bool dataGridLayoutAsked)
        {
            if (!dataGridLayoutAsked && !ignoreSelector)
            {
                // search layout for a selector first
                var selectorInterfaceType = typeof(ISelector<>).MakeGenericType(dataType);
                var selectorLayoutProvider = layoutProviders.FirstOrDefault(lp => selectorInterfaceType.IsAssignableFrom(lp.LayoutFor));
                var selectorLayout = selectorLayoutProvider?.GetCreateUpdateLayout(selectorLayoutProvider.LayoutFor, layoutProviders, options);
                if (selectorLayout != null)
                {
                    selectorLayout.SelectorType = selectorLayoutProvider.LayoutFor;
                    return selectorLayout;
                }
            }

            Func<ILayoutProvider, Type, Layout> getLayout = (lp, dt) => lp?.GetCreateUpdateLayout(dt, layoutProviders, options);
            if (dataGridLayoutAsked) getLayout = (lp, dt) => lp?.GetDataGridLayout(dt, layoutProviders, options);

            // search layout for the data type.
            var layout = getLayout(layoutProviders.FirstOrDefault(lp => lp.LayoutFor == dataType), realDataType);

            // search layout for the generic definition
            if (layout == null && dataType.IsGenericType) layout = Load(dataType.GetGenericTypeDefinition(), layoutProviders, ignoreCloseBehavior, realDataType, ignoreSelector, new Dictionary<string, object>(options), dataGridLayoutAsked);

            // search layouts for the base type
            if (layout == null && dataType.BaseType != typeof(object)) layout = Load(dataType.BaseType, layoutProviders, ignoreCloseBehavior, realDataType, ignoreSelector, new Dictionary<string, object>(options), dataGridLayoutAsked);

            if (layout == null)
            {
                // create a default layout
                var members = realDataType.GetMembers().Where(mb => !(mb is ConstructorInfo) && !(mb is FieldInfo) && !(mb is EventInfo) && mb.ReflectedType != typeof(object) && (!(mb is MethodInfo) || !((MethodInfo)mb).IsSpecialName)).OrderBy(mb => mb.Name).ToArray();
                if (dataGridLayoutAsked)
                    layout = new DataGridLayout(layoutProviders, options, members.Select(mb => new OneMemberLayout(layoutProviders, options, mb, false, null, null)), null, null);
                else
                    layout = new FlowLayout(layoutProviders, options, FlowDirectionEnum.Vertical, members.Select(mb => new OneMemberLayout(layoutProviders, options, mb, false, null, null)));
            }

            layout.IgnoreCloseBehavior = ignoreCloseBehavior;

            return layout;
        }
    }
}
