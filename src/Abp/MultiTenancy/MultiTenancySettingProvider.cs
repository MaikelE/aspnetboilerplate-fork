using Abp.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Abp.MultiTenancy
{
 
        /// <summary>
        /// Defines settings to use with multi tenancy.
        /// <see cref="MultiTenancySettingNames"/> for all available configurations.
        /// </summary>
        internal class MultiTenancySettingProvider : SettingProvider
        {
            public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
            {
                return new[]
                   {
                       new SettingDefinition(MultiTenancySettingNames.TenantConnectionString, ""),
                   };
            }
        }
    
}
