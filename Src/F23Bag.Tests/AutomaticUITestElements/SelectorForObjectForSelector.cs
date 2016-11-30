using F23Bag.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace F23Bag.Tests.AutomaticUITestElements
{
    public class SelectorForObjectForSelector : ISelector<ObjectForSelector>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObjectForSelector SelectedValue { get; set; }
    }
}
