using System;

namespace F23Bag.Domain
{
    public interface IHasInteractions
    {
        event EventHandler<InteractionEventArgs> InteractionsChanged;

        void InitializeInteractions();
    }
}
