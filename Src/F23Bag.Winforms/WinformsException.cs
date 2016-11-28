using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Winforms
{
    public class WinformsException : Exception
    {
        public WinformsException(string message)
            : base(message)
        { }
    }
}
