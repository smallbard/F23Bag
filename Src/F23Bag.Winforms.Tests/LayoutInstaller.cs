using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using F23Bag.AutomaticUI.Layouts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Winforms.Tests
{
    public class LayoutInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Classes.FromAssemblyInDirectory(new AssemblyFilter(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FilterByAssembly(a => !a.FullName.Contains("Microsoft.")))
                .BasedOn<ILayoutProvider>().WithServiceAllInterfaces());
        }
    }
}
