using System;
using System.Reflection;

namespace F23Bag.Domain
{
    public class InteractionEventArgs : EventArgs
    {
        public InteractionEventArgs(MemberInfo member, bool visible, bool enable)
        {
            Member = member;
            Visible = visible;
            Enabled = enable;
        }

        public MemberInfo Member { get; private set; }

        public bool Visible { get; private set; }

        public bool Enabled { get; private set; }
    }
}
