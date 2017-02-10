using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.AutomaticUI
{
    public class DefaultI18n : I18n
    {
        public string GetTranslation(string message)
        {
            return message;
        }

        public string GetTranslation(I18nMessage message)
        {
            return string.Format(message.CodeMessage, message.Parameters);
        }

        public CultureInfo CurrentFormatingCulture
        {
            get { return System.Threading.Thread.CurrentThread.CurrentCulture; }
            set
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = value;
                OnCurrentFormatingCultureChanged();
            }
        }

        public CultureInfo CurrentResourcingCulture
        {
            get { return System.Threading.Thread.CurrentThread.CurrentUICulture; }
            set
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = value;
                OnCurrentResourcingCultureChanged();
            }
        }

        public event EventHandler CurrentFormatingCultureChanged;

        public event EventHandler CurrentResourcingCultureChanged;

        protected virtual void OnCurrentFormatingCultureChanged()
        {
            CurrentFormatingCultureChanged?.Invoke(this, EventArgs.Empty);
        }
        
        protected virtual void OnCurrentResourcingCultureChanged()
        {
            CurrentResourcingCultureChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
