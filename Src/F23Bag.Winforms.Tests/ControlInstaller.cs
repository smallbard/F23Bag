using Castle.MicroKernel.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using System.Reflection;
using System.IO;
using F23Bag.Winforms.Controls;

namespace F23Bag.Winforms.Tests
{
    public class ControlInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Classes.FromAssemblyInDirectory(new AssemblyFilter(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FilterByAssembly(a => !a.FullName.Contains("Microsoft.")))
                .BasedOn<IControlConvention>().WithServiceAllInterfaces());
        }
    }
}
