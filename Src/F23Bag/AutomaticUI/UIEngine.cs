using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using F23Bag.AutomaticUI.Layouts;

namespace F23Bag.AutomaticUI
{
    /// <summary>
    /// Entry point for the automatic UI.
    /// </summary>
    public class UIEngine
    {
        private readonly IEnumerable<ILayoutProvider> _layoutProviders;
        private readonly IEnumerable<IAuthorization> _authorizations;
        private readonly IUIBuilder _uibuilder;

        public UIEngine(IEnumerable<ILayoutProvider> layoutProviders, IEnumerable<IAuthorization> authorizations, Func<Func<Type, IAuthorization>, IUIBuilder> getUiBuilder)
        {
            _layoutProviders = layoutProviders ?? new ILayoutProvider[] { };
            _authorizations = authorizations ?? new IAuthorization[] { };
            _uibuilder = getUiBuilder(GetAuthorization);
        }

        public void Display(object data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            Display(data, data.ToString());
        }

        public void Display(object data, string label)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var layouts = Layout.Load(data.GetType(), _layoutProviders);
            var layout = layouts.SkipWhile(l => l is DataGridLayout).FirstOrDefault() ?? layouts.First();
            _uibuilder.Display(layout, data, label);
        }

        public IAuthorization GetAuthorization(Type dataType)
        {
            for (var type = dataType; type != null; type = type.BaseType)
            {
                var authorization = _authorizations.FirstOrDefault(auth => auth.Accept(dataType));
                if (authorization != null) return authorization;
            }

            return new DefaultAuthorization(dataType);
        }      

        private class DefaultAuthorization : IAuthorization
        {
            private Type _dataType;

            public DefaultAuthorization(Type dataType)
            {
                _dataType = dataType;
            }

            public bool Accept(Type type)
            {
                return _dataType.Equals(type);
            }

            public bool IsEditable(object data, PropertyInfo property)
            {
                return property.GetSetMethod() != null || typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType);
            }

            public bool IsEnable(object data, MethodInfo method)
            {
                return true;
            }

            public bool IsVisible(object data, MemberInfo member)
            {
                return true;
            }
        }
    }
}
