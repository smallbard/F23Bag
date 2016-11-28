using System;

namespace F23Bag.Data
{
    public class SQLException : Exception
    {
        public SQLException(string message)
            : base(message)
        { }
    }
}
