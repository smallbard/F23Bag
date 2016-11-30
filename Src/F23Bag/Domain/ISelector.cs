using System.ComponentModel;

namespace F23Bag.Domain
{
    public interface ISelector<TData> : INotifyPropertyChanged
    {
        TData SelectedValue { get; set; }
    }
}
