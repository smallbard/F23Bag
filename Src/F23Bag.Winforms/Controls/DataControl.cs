using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using F23Bag.Domain;
using F23Bag.AutomaticUI;

namespace F23Bag.Winforms.Controls
{
    public class DataControl : UserControl
    {
        protected static readonly Color cstForeColor = Color.FromArgb(-16757865);
        protected static readonly Color cstBackColor = Color.White;
        private IHasValidation _dataWithValidation;
        private IHasInteractions _dataWithInteractions;

        public DataControl()
        {
            BackColor = cstBackColor;
            ForeColor = cstForeColor;
        }

        public MemberInfo DisplayedMember { get; protected set; }

        protected virtual PictureBox ValidationIcon { get { return null; } }

        public void Display(object data, I18n i18n, Func<Type, IAuthorization> getAuthorization)
        {
            if (data is IHasValidation && ValidationIcon != null)
            {
                _dataWithValidation = (IHasValidation)data;
                _dataWithValidation.ValidationInfoCreated += DataControl_ValidationInfoCreated;
            }

            CustomDisplay(data, i18n);

            var authorization = getAuthorization(data.GetType());
            Visible = authorization.IsVisible(data, DisplayedMember);
            if (DisplayedMember is PropertyInfo)
                Enabled = Enabled && authorization.IsEditable(data, (PropertyInfo)DisplayedMember);
            else if (DisplayedMember is MethodInfo)
                Enabled = Enabled && authorization.IsEnable(data, (MethodInfo)DisplayedMember);

            if (data is IHasInteractions)
            {
                _dataWithInteractions = (IHasInteractions)data;
                _dataWithInteractions.InteractionsChanged += DataControl_InteractionsChanged;
                _dataWithInteractions.InitializeInteractions();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _dataWithValidation != null) _dataWithValidation.ValidationInfoCreated -= DataControl_ValidationInfoCreated;
            if (disposing && _dataWithInteractions != null) _dataWithInteractions.InteractionsChanged -= DataControl_InteractionsChanged;
            base.Dispose(disposing);
        }

        protected virtual void CustomDisplay(object data, I18n i18n)
        {
            throw new NotImplementedException();
        }

        private void DataControl_InteractionsChanged(object sender, InteractionEventArgs e)
        {
            if (DisplayedMember == null || !DisplayedMember.Equals(e.Member)) return;

            Visible = e.Visible;
            Enabled = e.Enabled;
        }

        private void DataControl_ValidationInfoCreated(object sender, ValidationEventArgs e)
        {
            if (DisplayedMember == null || !DisplayedMember.Equals(e.Property)) return;

            var act = new Action(() =>
            {
                if (ValidationIcon.Image != null) ValidationIcon.Image.Dispose();

                if (e.Level == ValidationLevel.None)
                {
                    ValidationIcon.Visible = false;
                    return;
                }

                var iconSize = SystemInformation.SmallIconSize;
                var bitmap = new Bitmap(iconSize.Width, iconSize.Height);

                using (var g = Graphics.FromImage(bitmap))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    var icon = SystemIcons.Error;
                    if (e.Level == ValidationLevel.Warning)
                        icon = SystemIcons.Warning;
                    else if (e.Level == ValidationLevel.Information)
                        icon = SystemIcons.Information;

                    g.DrawImage(icon.ToBitmap(), new Rectangle(Point.Empty, iconSize));
                }

                new ToolTip().SetToolTip(ValidationIcon, e.Message);
                ValidationIcon.Image = bitmap;
                ValidationIcon.Visible = true;
            });

            if (ValidationIcon.InvokeRequired)
                ValidationIcon.Invoke(act);
            else
                act();
        }
    }
}
