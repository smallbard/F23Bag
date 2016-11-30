using System;
using System.Collections.Generic;
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
    }
}
