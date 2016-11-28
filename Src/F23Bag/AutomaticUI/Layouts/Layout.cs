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

        protected bool IgnoreCloseBehavior { get; private set; }

        public IEnumerable<Layout> LoadSubLayout(Type dataType, bool ignoreCloseBehavior, bool ignoreSelector)
        {
            return Load(dataType, _layoutProviders, ignoreCloseBehavior, dataType, ignoreSelector);
        }

        public static IEnumerable<Layout> Load(Type dataType, IEnumerable<ILayoutProvider> layoutProviders)
        {
            return Load(dataType, layoutProviders, false, dataType, true);
        }

        private static IEnumerable<Layout> Load(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, bool ignoreCloseBehavior, Type realDataType, bool ignoreSelector)
        {
            IEnumerable<Layout> layouts = null;

            if (!ignoreSelector)
            {
                var selectorLayoutProvider = layoutProviders.FirstOrDefault(lp => typeof(ISelector<>).MakeGenericType(dataType).IsAssignableFrom(lp.LayoutFor));
                layouts = selectorLayoutProvider?.GetLayouts(selectorLayoutProvider?.LayoutFor, layoutProviders);
            }

            var noLayout = layouts == null || !layouts.Any();
            if (noLayout)
            {
                layouts = layoutProviders.FirstOrDefault(lp => lp.LayoutFor == dataType)?.GetLayouts(realDataType, layoutProviders);
                noLayout = layouts == null || !layouts.Any();
            }

            if (noLayout && dataType.IsGenericType) layouts = Load(dataType.GetGenericTypeDefinition(), layoutProviders, ignoreCloseBehavior, realDataType, ignoreSelector);
            if (noLayout && dataType.BaseType != typeof(object)) layouts = Load(dataType.BaseType, layoutProviders, ignoreCloseBehavior, realDataType, ignoreSelector);

            if (layouts == null || !layouts.Any())
            {
                var members = realDataType.GetMembers().Where(mb => !(mb is ConstructorInfo) && !(mb is FieldInfo) && mb.DeclaringType != typeof(object) && (!(mb is MethodInfo) || !((MethodInfo)mb).IsSpecialName)).OrderBy(mb => mb.Name).ToArray();
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
