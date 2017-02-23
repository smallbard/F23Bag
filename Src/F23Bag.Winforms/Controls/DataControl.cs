using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using F23Bag.Domain;
using F23Bag.AutomaticUI.Layouts;
using System.ComponentModel;
using F23Bag.AutomaticUI;

namespace F23Bag.Winforms.Controls
{
    public class DataControl : UserControl
    {
        protected static readonly Color cstForeColor = Color.FromArgb(-16757865);
        protected static readonly Color cstBackColor = Color.White;
        private readonly Layout _layout;
        private MemberController _memberController;
        private IHasInteractions _dataWithInteractions;

        public DataControl(Layout layout, WinformContext context)
        {
            _layout = layout;
            Context = context;
            BackColor = cstBackColor;
            ForeColor = cstForeColor;
        }

        public MemberInfo DisplayedMember { get; protected set; }

        protected WinformContext Context { get; private set; }

        protected virtual PictureBox ValidationIcon { get { return null; } }

        public void Display(object data)
        {
            if (DisplayedMember != null)
            {
                _memberController = Context.Engine.GetController(DisplayedMember, data, _layout, Context.Resolve);
                if (ValidationIcon != null) _memberController.ValidationInfoCreated += DataControl_ValidationInfoCreated;

                var authorization = _memberController.Authorization;
                Visible = authorization.IsVisible(data, DisplayedMember);
                if (DisplayedMember is PropertyInfo)
                    Enabled = Enabled && authorization.IsEditable(data, (PropertyInfo)DisplayedMember);
                else if (DisplayedMember is MethodInfo)
                    Enabled = Enabled && authorization.IsEnable(data, (MethodInfo)DisplayedMember);

                data = _memberController.DisplayedObject;
            }

            CustomDisplay(data);

            if (data is IHasInteractions)
            {
                _dataWithInteractions = (IHasInteractions)data;
                _dataWithInteractions.InteractionsChanged += DataControl_InteractionsChanged;
                _dataWithInteractions.InitializeInteractions();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _memberController?.Dispose();
                if (_dataWithInteractions != null) _dataWithInteractions.InteractionsChanged -= DataControl_InteractionsChanged;
            }

            base.Dispose(disposing);
        }

        protected virtual void CustomDisplay(object data)
        {
            throw new NotImplementedException();
        }

        private void DataControl_InteractionsChanged(object sender, InteractionEventArgs e)
        {
            if (DisplayedMember == null || !DisplayedMember.Equals(e.Member)) return;

            var act = new Action(() =>
            {
                Visible = e.Visible;
                Enabled = e.Enabled;
            });

            if (InvokeRequired)
                Invoke(act);
            else
                act();
        }

        private void DataControl_ValidationInfoCreated(object sender, ValidationEventArgs e)
        {
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

                new ToolTip().SetToolTip(ValidationIcon, Context.I18n.GetTranslation(e));
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
