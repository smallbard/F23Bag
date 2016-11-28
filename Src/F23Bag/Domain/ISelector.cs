namespace F23Bag.Domain
{
    public interface ISelector<TData>
    {
        TData SelectedValue { get; set; }
    }
}
