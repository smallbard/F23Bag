using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Castle.Windsor.Proxy;
using F23Bag.AutomaticUI;
using F23Bag.AutomaticUI.Layouts;
using F23Bag.Domain;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace F23Bag.Winforms.Tests
{
    static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var container = new WindsorContainer(new DefaultProxyFactory(disableSignedModule: true));
            container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));
            container.Install(FromAssembly.InThisApplication());

            new UIEngine(container.ResolveAll<ILayoutProvider>(), null, null, new WinformsUIBuilder(container.ResolveAll<IControlConvention>(), true)).Display(new Test1() { Prop1 = "Test" });
        }
    }

    public class Test1 : IHasValidation, IHasInteractions
    {
        public Test1()
        {
            StringFromList = "two";
            Prop2 = DateTime.Now;
        }

        public string Prop1 { get; set; }

        public DateTime Prop2 { get; set; }

        public int Prop3 { get; set; }

        public double Prop4 { get; set; }

        public Test2 Prop5 { get; set; }

        public ObservableCollection<Test2> Tests { get; private set; } = new ObservableCollection<Test2>();

        public TestEnum? EnumValue { get; set; }

        public TestEnum EnumValueNotNull { get; set; }

        public string StringFromList { get; set; }

        public string[] ListForString { get { return new[] { "one", "two", "three" }; } }

        public event EventHandler<ValidationEventArgs> ValidationInfoCreated;

        public event EventHandler<InteractionEventArgs> InteractionsChanged;

        public Test2 Test()
        {
            var t2 = new Test2() { Prop2 = "hoho", PropInt = 5 };
            Tests.Add(t2);

            ValidationInfoCreated?.Invoke(this, new ValidationEventArgs(ValidationLevel.Error, GetType().GetProperty(nameof(Prop1)), "Big mistake!"));
            ValidationInfoCreated?.Invoke(this, new ValidationEventArgs(ValidationLevel.Information, GetType().GetProperty(nameof(Prop3)), "You know..."));
            ValidationInfoCreated?.Invoke(this, new ValidationEventArgs(ValidationLevel.Warning, GetType().GetProperty(nameof(Prop4)), "Be carreful..."));
            InteractionsChanged?.Invoke(this, new InteractionEventArgs(GetType().GetProperty(nameof(StringFromList)), false, false));

            return t2;
        }

        public void InitializeInteractions()
        {
            InteractionsChanged?.Invoke(this, new InteractionEventArgs(GetType().GetProperty(nameof(Prop2)), true, false));
        }
    }

    public enum TestEnum
    {
        One,
        Two,
        Three
    }

    public class Test2 : INotifyPropertyChanged
    {
        private string _prop2;

        public string Prop2
        {
            get { return _prop2; }
            set
            {
                _prop2 = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Prop2"));
            }
        }

        public int PropInt { get; set; }

        public TestEnum? EnumValue { get; set; }

        public TestEnum EnumValueNotNull { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Test(Test3 t3, Test4 t4)
        {
            Prop2 = t3.Name + " : " + t4.Value.ToString();
        }

        public Test2 Open()
        {
            Prop2 = "Opened by open";
            return this;
        }
    }

    public class Test3
    {
        public string Name { get; set; }

        public string Description { get; set; }
    }

    public class Test4
    {
        public int Value { get; set; }
    }
}
