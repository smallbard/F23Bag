using System;

namespace F23Bag.Domain
{
    public interface IHasValidation
    {
        event EventHandler<ValidationEventArgs> ValidationInfoCreated;
    }
}
