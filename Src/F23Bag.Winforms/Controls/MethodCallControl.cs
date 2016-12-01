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
        private readonly string _label;

        public MethodCallControl(OneMemberLayout layout, WinformContext context, MethodInfo method, bool hasCloseBehavior, string label)
            : base(layout, context)
        {
            InitializeComponent();

            btnMethodCall.FlatStyle = FlatStyle.Flat;
            btnMethodCall.BackColor = System.Drawing.Color.White;

            DisplayedMember = method;
            _layout = layout;
            _method = method;
            _hasCloseBehavior = hasCloseBehavior;
            _label = label;
        }

        protected override void CustomDisplay(object data)
        {
            btnMethodCall.Text = Context.I18n.GetTranslation(_label);
            btnMethodCall.Click += (s, e) =>
            {
                if (_method.DeclaringType.IsAssignableFrom(data.GetType()))
                {
                    var parameters = AskParameters(_layout, _method, Context);
                    CallMethod(_layout, data, parameters, _method, _hasCloseBehavior, Context);
                }
                else if (data is Func<System.Collections.IEnumerable>)
                {
                    var lst = ((Func<System.Collections.IEnumerable>)data)().OfType<object>().ToArray();
                    if (lst.Length == 0) return;
                    var parameters = AskParameters(_layout, _method, Context);
                    foreach (var d in lst)
                        if (d != null)
                        {
                            var authorization = Context.GetAuthorization(d.GetType());
                            if (authorization.IsEnable(d, _method)) CallMethod(_layout, d, parameters, _method, false, Context);
                        }
                }
            };
        }

        internal static void CallMethod(Layout layout, object data, List<object> parameters, MethodInfo method, bool hasCloseBehavior, WinformContext context)
        {
            var returnValue = method.Invoke(data, parameters.ToArray());
            if (hasCloseBehavior && ((returnValue is bool && (bool)returnValue) || !(returnValue is bool)) && Form.ActiveForm != null) Form.ActiveForm.Close();
            if (returnValue != null && !(returnValue is bool))
            {
                var builder = new WinformsUIBuilder(context.UIBuilder.ControlConventions, false, context.Resolve, context.I18n, context.GetAuthorization);
                builder.Display(layout.LoadSubLayout(returnValue.GetType(), false, true).SkipWhile(l => l is F23Bag.AutomaticUI.Layouts.DataGridLayout).First(),returnValue, returnValue.ToString());
            }
        }

        internal static List<object> AskParameters(Layout layout, MethodInfo method, WinformContext context)
        {
            var parameters = new List<object>();
            foreach (var parameter in method.GetParameters())
            {
                object argument = null;
                if (parameter.ParameterType.IsInterface || parameter.ParameterType.Assembly.FullName.StartsWith("F23Bag"))
                {
                    argument = context.Resolve(parameter.ParameterType);
                }
                else
                {
                    argument = Activator.CreateInstance(parameter.ParameterType);

                    var builder = new WinformsUIBuilder(context.UIBuilder.ControlConventions, false, context.Resolve, context.I18n, context.GetAuthorization);
                    builder.Display(layout.LoadSubLayout(parameter.ParameterType, false, false).SkipWhile(l => l is F23Bag.AutomaticUI.Layouts.DataGridLayout).First(), argument, method.DeclaringType.FullName + "." + method.Name + "." + parameter.Name);
                }
                parameters.Add(argument);
            }

            return parameters;
        }
    }
}
