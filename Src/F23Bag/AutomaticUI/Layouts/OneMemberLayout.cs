using System.Collections.Generic;
using System.Reflection;

namespace F23Bag.AutomaticUI.Layouts
{
    /// <summary>
    /// A simple layout for one type membre.
    /// </summary>
    public class OneMemberLayout : Layout
    {
        private readonly bool _hasCloseBehavior;

        internal OneMemberLayout(IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options, MemberInfo member, bool hasCloseBehavior, string label, MemberInfo itemsSource)
            : base(layoutProviders, options)
        {
            Member = member;
            _hasCloseBehavior = hasCloseBehavior;
            Label = label ?? member.DeclaringType.Name + "." + member.Name;
            ItemsSource = itemsSource;
        }

        public MemberInfo Member { get; private set; }

        public bool HasCloseBehavior
        {
            get
            {
                return _hasCloseBehavior && !IgnoreCloseBehavior;
            }
        }

        public string Label { get; private set; }

        public MemberInfo ItemsSource { get; private set; }

        public bool IsEditable
        {
            get
            {
                return Member is PropertyInfo && ((PropertyInfo)Member).GetSetMethod() != null;
            }
        }
    }
}
