using F23Bag.AutomaticUI.Layouts;
using F23Bag.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.AutomaticUI
{
    public class MemberController : IDisposable
    {
        private readonly MemberInfo _member;
        private readonly object _owner;
        private readonly object _selector;
        private readonly UIEngine _engine;

        internal MemberController(MemberInfo member, object owner, Layout layout, UIEngine engine, Func<Type, object> resolve)
        {
            _member = member;
            _owner = owner;
            _engine = engine;

            if (_member is PropertyInfo)
            {
                if (layout.SelectorType != null)
                {
                    _selector = resolve(layout.SelectorType);

                    var selectedValueProperty = layout.SelectorType.GetProperty(nameof(ISelector<object>.SelectedValue));
                    selectedValueProperty.SetValue(_selector, layout.SelectorOriginalProperty.GetValue(owner));

                    var npc = _selector as INotifyPropertyChanged;
                    if (npc == null) throw new LayoutException($"{layout.SelectorType} must implement INotifyPropertyChanged");
                    npc.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ISelector<object>.SelectedValue)) layout.SelectorOriginalProperty.SetValue(_owner, selectedValueProperty.GetValue(_selector));
                    };
                }

                if (_owner is IHasValidation)
                {
                    ((IHasValidation)_owner).ValidationInfoCreated += PropertyController_ValidationInfoCreated;
                }
            }
        }

        public event EventHandler<ValidationEventArgs> ValidationInfoCreated;

        public object DisplayedObject => _selector ?? _owner;

        public IAuthorization Authorization => _engine.GetAuthorization(_owner.GetType());

        protected virtual void OnValidationInfoCreated(ValidationEventArgs e)
        {
            ValidationInfoCreated?.Invoke(_owner, e);
        }

        private void PropertyController_ValidationInfoCreated(object sender, ValidationEventArgs e)
        {
            if (_member.Equals(e.Property)) OnValidationInfoCreated(e);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_owner is IHasValidation)
                    {
                        ((IHasValidation)_owner).ValidationInfoCreated -= PropertyController_ValidationInfoCreated;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
