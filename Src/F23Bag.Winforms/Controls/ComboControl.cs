using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;
using F23Bag.AutomaticUI;

namespace F23Bag.Winforms.Controls
{
    public partial class ComboControl : DataControl
    {
        private PropertyInfo _property;
        private PropertyInfo _itemsSource;
        private readonly string _label;
        private object _data;

        public ComboControl()
        {
            InitializeComponent();

            cbValue.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        public ComboControl(PropertyInfo property, string label, PropertyInfo itemsSource)
            : this()
        {
            DisplayedMember = property;
            _property = property;
            _label = label;
            _itemsSource = itemsSource;
        }

        protected override PictureBox ValidationIcon
        {
            get
            {
                return _validationIcon;
            }
        }

        protected override void CustomDisplay(object data, I18n i18n)
        {
            lblLabel.Text = i18n.GetTranslation(_label);

            cbValue.Items.Clear();
            foreach (var item in (System.Collections.IEnumerable)_itemsSource.GetValue(data)) cbValue.Items.Add(item);

            _data = data;
            cbValue.SelectedItem = _property.GetValue(data);

            if (data is INotifyPropertyChanged)
            {
                ((INotifyPropertyChanged)data).PropertyChanged -= ComboControl_PropertyChanged;
                ((INotifyPropertyChanged)data).PropertyChanged += ComboControl_PropertyChanged;
            }

            cbValue.SelectedValueChanged -= ComboValue_SelectedValueChanged;
            cbValue.SelectedValueChanged += ComboValue_SelectedValueChanged;
        }

        private void ComboControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _property.Name)
            {
                Action act = () => cbValue.SelectedItem = _property.GetValue(_data);
                if (InvokeRequired)
                    Invoke(act);
                else
                    act();
            }
        }

        private void ComboValue_SelectedValueChanged(object sender, EventArgs e)
        {
            _property.SetValue(_data, cbValue.SelectedItem);
        }
    }
}
