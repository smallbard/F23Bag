using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.AutomaticUI
{
    public interface I18nMessage
    {
        string CodeMessage { get; }

        object Parameters { get; }
    }
}
