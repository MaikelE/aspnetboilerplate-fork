using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.MultiTenancy;
using Castle.MicroKernel.Registration;
using System;
using System.Configuration;
using System.Linq;

namespace Abp.EntityFramework.Dependency
{
    /// <summary>
    /// Registers classes derived from AbpDbContext with configurations.
    /// </summary>
    public class EntityFrameworkConventionalRegisterer : IConventionalDependencyRegistrar
    {
        public void RegisterAssembly(IConventionalRegistrationContext context)
        {
            //TODO: Consider to change in registering IMultiTenancySplitContext apart from abpDbContext
            context.IocManager.IocContainer.Register(
                Classes.FromAssembly(context.Assembly)
                    .IncludeNonPublicTypes()
                    .BasedOn<AbpDbContext>()
                    .WithServiceSelf()
                    .LifestyleTransient()
                    .Configure(c => c.DynamicParameters(
                        (kernel, dynamicParams) =>
                        {
                            var connectionString = GetNameOrConnectionStringOrNull(c.Implementation, context.IocManager);
                            if (!string.IsNullOrWhiteSpace(connectionString))
                            {
                                dynamicParams["nameOrConnectionString"] = connectionString;
                            }
                        })));
            
        }

        private static string GetNameOrConnectionStringOrNull(Type implementation, IIocResolver iocResolver)
        {

            if (implementation.GetInterfaces().Contains(typeof(IMultiTenancySplitContext)))
            {
                if (iocResolver.IsRegistered<IMultiTenancyConfig>())
                {
                    var multitenancy = iocResolver.Resolve<IMultiTenancyConfig>();
                    if (multitenancy.IsEnabled)
                    {

                        if (iocResolver.IsRegistered<ISettingManager>())
                        {
                            var defaultConnectionStringTenant = iocResolver.Resolve<ISettingManager>().GetSettingValue(MultiTenancySettingNames.TenantConnectionString);
                            if (!string.IsNullOrWhiteSpace(defaultConnectionStringTenant))
                            {
                                return defaultConnectionStringTenant;
                            }
                        }

                        var defaultConnectionString = multitenancy.DefaultNameOrConnectionStringTenant;
                        if (!string.IsNullOrWhiteSpace(defaultConnectionString))
                        {
                            return defaultConnectionString;
                        }
                    }
                }
            }
            else
            {
                //Userclasses
                if (iocResolver.IsRegistered<IAbpStartupConfiguration>())
                {
                    var defaultConnectionString = iocResolver.Resolve<IAbpStartupConfiguration>().DefaultNameOrConnectionString;
                    if (!string.IsNullOrWhiteSpace(defaultConnectionString))
                    {
                        return defaultConnectionString;
                    }
                }

                if (ConfigurationManager.ConnectionStrings.Count == 1)
                {
                    return ConfigurationManager.ConnectionStrings[0].Name;
                }

                if (ConfigurationManager.ConnectionStrings["Default"] != null)
                {
                    return "Default";
                }
            }
            return null;
        }
    }
}