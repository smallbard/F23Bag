using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using F23Bag.AutomaticUI;
using F23Bag.AutomaticUI.Layouts;

namespace F23Bag.Winforms.Controls
{
    public partial class EnumControl : DataControl
    {
        private readonly PropertyInfo _property;
        private readonly string _label;
        private object _data;

        public EnumControl(Layout layout, WinformContext context, PropertyInfo property, string label)
            : base(layout, context)
        {
            InitializeComponent();

            cbValue.DropDownStyle = ComboBoxStyle.DropDownList;

            DisplayedMember = property;
            _property = property;
            _label = label;
        }

        protected override PictureBox ValidationIcon
        {
            get
            {
                return _validationIcon;
            }
        }

        protected override void CustomDisplay(object data)
        {
            lblLabel.Text = Context.I18n.GetTranslation(_label);

            cbValue.Items.Clear();
            var enumType = _property.PropertyType.IsGenericType ? _property.PropertyType.GetGenericArguments()[0] : _property.PropertyType;
            if (_property.PropertyType.IsGenericType) cbValue.Items.Add(new EnumValue(null, ""));
            foreach (var enumValue in Enum.GetValues(enumType)) cbValue.Items.Add(new EnumValue(enumValue, Context.I18n.GetTranslation(enumType.Name + "." + enumValue.ToString())));

            _data = data;
            var value = _property.GetValue(data);
            cbValue.SelectedItem = cbValue.Items.OfType<EnumValue>().First(ev => Equals(ev.Value, value));

            if (data is INotifyPropertyChanged)
            {
                ((INotifyPropertyChanged)data).PropertyChanged -= EnumControl_PropertyChanged;
                ((INotifyPropertyChanged)data).PropertyChanged += EnumControl_PropertyChanged;
            }

            cbValue.SelectedValueChanged -= CbValue_SelectedValueChanged;
            cbValue.SelectedValueChanged += CbValue_SelectedValueChanged;
        }

        private void EnumControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _property.Name)
            {
                Action act = () =>
                {
                    var value = _property.GetValue(_data);
                    cbValue.SelectedItem = value == null ? null : cbValue.Items.OfType<EnumValue>().First(ev => ev.Value.Equals(value));
                };

                if (InvokeRequired)
                    Invoke(act);
                else
                    act();
            }
        }

        private void CbValue_SelectedValueChanged(object sender, EventArgs e)
        {
            var selectedValue = (EnumValue)cbValue.SelectedItem;
            _property.SetValue(_data, selectedValue.Value);
        }

        private class EnumValue
        {
            public EnumValue(object enumValue, string label)
            {
                Value = enumValue;
                Label = label;
            }

            public object Value { get; private set; }

            public string Label { get; private set; }

            public override string ToString()
            {
                return Label;
            }
        }
    }
}
