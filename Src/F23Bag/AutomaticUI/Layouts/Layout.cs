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

        protected Layout(IEnumerable<ILayoutProvider> layoutProviders)
        {
            _layoutProviders = layoutProviders;
        }

        public Type SelectorType { get; private set; }
        
        internal Dictionary<string, object> Options { get; set; }

        protected bool IgnoreCloseBehavior { get; private set; }

        public IEnumerable<Layout> LoadSubLayout(Type dataType, bool ignoreCloseBehavior, bool ignoreSelector)
        {
            return Load(dataType, _layoutProviders, ignoreCloseBehavior, dataType, ignoreSelector, Options);
        }

        public static IEnumerable<Layout> Load(Type dataType, IEnumerable<ILayoutProvider> layoutProviders)
        {
            return Load(dataType, layoutProviders, false, dataType, true, new Dictionary<string, object>());
        }

        private static IEnumerable<Layout> Load(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, bool ignoreCloseBehavior, Type realDataType, bool ignoreSelector, Dictionary<string,object> options)
        {
            IEnumerable<Layout> layouts = null;

            if (!ignoreSelector)
            {
                // search layouts for a selector first
                var selectorInterfaceType = typeof(ISelector<>).MakeGenericType(dataType);
                var selectorLayoutProvider = layoutProviders.FirstOrDefault(lp => selectorInterfaceType.IsAssignableFrom(lp.LayoutFor));
                layouts = selectorLayoutProvider?.GetLayouts(selectorLayoutProvider?.LayoutFor, layoutProviders, options).ToList();

                if (layouts != null)
                    foreach (var layout in layouts)
                        layout.SelectorType = selectorLayoutProvider.LayoutFor;
            }

            var noLayout = layouts == null || !layouts.Any();
            if (noLayout)
            {
                // search layouts for the data type.
                layouts = layoutProviders.FirstOrDefault(lp => lp.LayoutFor == dataType)?.GetLayouts(realDataType, layoutProviders, options).ToList();
                noLayout = layouts == null || !layouts.Any();
            }

            // search layouts for the generic definiion
            if (noLayout && dataType.IsGenericType) layouts = Load(dataType.GetGenericTypeDefinition(), layoutProviders, ignoreCloseBehavior, realDataType, ignoreSelector, new Dictionary<string, object>(options));

            // search layouts for the base type
            if (noLayout && dataType.BaseType != typeof(object)) layouts = Load(dataType.BaseType, layoutProviders, ignoreCloseBehavior, realDataType, ignoreSelector, new Dictionary<string, object>(options));

            if (layouts == null || !layouts.Any())
            {
                // create a default layout
                var members = realDataType.GetMembers().Where(mb => !(mb is ConstructorInfo) && !(mb is FieldInfo) && !(mb is EventInfo) && mb.DeclaringType != typeof(object) && (!(mb is MethodInfo) || !((MethodInfo)mb).IsSpecialName)).OrderBy(mb => mb.Name).ToArray();
                layouts = new[] { new FlowLayout(layoutProviders, FlowDirectionEnum.Vertical, members.Select(mb => new OneMemberLayout(layoutProviders, mb, false, null, null))) };
            }

            foreach (var layout in layouts)
            {
                layout.IgnoreCloseBehavior = ignoreCloseBehavior;
            }

            return layouts;
        }
    }
}
