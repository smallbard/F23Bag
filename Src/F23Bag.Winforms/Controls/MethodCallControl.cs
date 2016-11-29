using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using F23Bag.AutomaticUI;
using F23Bag.AutomaticUI.Layouts;

namespace F23Bag.Winforms.Controls
{
    public partial class MethodCallControl : DataControl
    {
        private readonly OneMemberLayout _layout;
        private readonly MethodInfo _method;
        private readonly bool _hasCloseBehavior;
        private readonly WinformsUIBuilder _uiBuilder;
        private readonly string _label;
        private readonly Func<Type, IAuthorization> _getAuthorization;

        public MethodCallControl(OneMemberLayout layout, MethodInfo method, bool hasCloseBehavior, WinformsUIBuilder uiBuilder, string label, Func<Type, IAuthorization> getAuthorization)
        {
            InitializeComponent();

            btnMethodCall.FlatStyle = FlatStyle.Flat;
            btnMethodCall.BackColor = System.Drawing.Color.White;

            DisplayedMember = method;
            _layout = layout;
            _method = method;
            _hasCloseBehavior = hasCloseBehavior;
            _uiBuilder = uiBuilder;
            _label = label;
            _getAuthorization = getAuthorization;
        }

        protected override void CustomDisplay(object data, I18n i18n)
        {
            btnMethodCall.Text = i18n.GetTranslation(_label);
            btnMethodCall.Click += (s, e) =>
            {
                if (_method.DeclaringType.IsAssignableFrom(data.GetType()))
                {
                    var parameters = AskParameters(_layout, _method, _uiBuilder, i18n, _getAuthorization);
                    CallMethod(_layout, data, parameters, _method, _hasCloseBehavior, _uiBuilder, i18n, _getAuthorization);
                }
                else if (data is Func<System.Collections.IEnumerable>)
                {
                    var lst = ((Func<System.Collections.IEnumerable>)data)().OfType<object>().ToArray();
                    if (lst.Length == 0) return;
                    var parameters = AskParameters(_layout, _method, _uiBuilder, i18n, _getAuthorization);
                    foreach (var d in lst)
                        if (d != null)
                        {
                            var authorization = _getAuthorization(d.GetType());
                            if (authorization.IsEnable(d, _method)) CallMethod(_layout, d, parameters, _method, false, _uiBuilder, i18n, _getAuthorization);
                        }
                }
            };
        }

        internal static void CallMethod(Layout layout, object data, List<object> parameters, MethodInfo method, bool hasCloseBehavior, WinformsUIBuilder uiBuilder, I18n i18n, Func<Type, IAuthorization> getAuthorization)
        {
            var returnValue = method.Invoke(data, parameters.ToArray());
            if (hasCloseBehavior && ((returnValue is bool && (bool)returnValue) || !(returnValue is bool)) && Form.ActiveForm != null) Form.ActiveForm.Close();
            if (returnValue != null && !(returnValue is bool))
            {
                var builder = new WinformsUIBuilder(uiBuilder.ControlConventions, false);
                builder.Display(layout.LoadSubLayout(returnValue.GetType(), false, true).SkipWhile(l => l is F23Bag.AutomaticUI.Layouts.DataGridLayout).First(),returnValue, returnValue.ToString(), i18n, getAuthorization);
            }
        }

        internal static List<object> AskParameters(Layout layout, MethodInfo method, WinformsUIBuilder uiBuilder, I18n i18n, Func<Type, IAuthorization> getAuthorization)
        {
            var parameters = new List<object>();
            foreach (var parameter in method.GetParameters())
            {
                var argument = Activator.CreateInstance(parameter.ParameterType);

                var builder = new WinformsUIBuilder(uiBuilder.ControlConventions, false);
                builder.Display(layout.LoadSubLayout(parameter.ParameterType, false, false).SkipWhile(l => l is F23Bag.AutomaticUI.Layouts.DataGridLayout).First(),argument, method.DeclaringType.FullName + "." + method.Name + "." + parameter.Name, i18n, getAuthorization);
                parameters.Add(argument);
            }

            return parameters;
        }
    }
}
