using System.Reflection;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.EntityFramework.Dependency;
using Abp.Tests;
using Shouldly;
using Xunit;
using Abp.MultiTenancy;
using Abp.Configuration;
using System.Collections.Generic;
using NSubstitute;
using System.Threading.Tasks;
using System.Linq;
using Abp.Tests.Configuration;
using Abp.Runtime.Session;

namespace Abp.EntityFramework.Tests.Repositories
{
    public class EntityFrameworkConventionalRegistererMultiple_Test : TestBaseWithLocalIocManager
    {
        [Fact]
        public void Should_Set_ConnectionString_If_Configured()
        {
            new EntityFrameworkConventionalRegisterer()
                .RegisterAssembly(
                    new ConventionalRegistrationContext(
                        Assembly.GetExecutingAssembly(),
                        LocalIocManager,
                        new ConventionalRegistrationConfig()
                        ));

            //Should call default constructor since IAbpStartupConfiguration is not configured. 
            var context1 = LocalIocManager.Resolve<MyDbContext>();
            context1.CalledConstructorWithConnectionString.ShouldBe(false);            

            var contextApp = LocalIocManager.Resolve<MyAppDbContext>();
            contextApp.CalledConstructorWithConnectionString.ShouldBe(false);
            
            //var session = new MyChangableSession();

            //var settingManager = new SettingManager(CreateMockSettingDefinitionManager());
            //settingManager.SettingStore = new MemorySettingStore();
            //settingManager.AbpSession = session;
            //session.TenantId = 1;
            //session.UserId = 1;


            //LocalIocManager.Register<ISettingStore, MyChangableSession>();
           
            LocalIocManager.Register<IMultiTenancyConfig, MultiTenancyConfig>();

            var contextAppWithConfigNotEnabled = LocalIocManager.Resolve<MyAppDbContext>();
            contextAppWithConfigNotEnabled.CalledConstructorWithConnectionString.ShouldBe(false);

            string conn3 = context1.Database.Connection.ConnectionString.ToString();

            LocalIocManager.Resolve<IMultiTenancyConfig>().IsEnabled = true;

            var contextAppWithConfigEnabledNoConString = LocalIocManager.Resolve<MyAppDbContext>();
            contextAppWithConfigNotEnabled.CalledConstructorWithConnectionString.ShouldBe(false);

            LocalIocManager.Resolve<IMultiTenancyConfig>().DefaultNameOrConnectionStringTenant = "Server=localhost;Database=TenantContext;User=sa;Password=123";

            var contextAppWithConfigEnabledAndConString = LocalIocManager.Resolve<MyAppDbContext>();
            contextAppWithConfigEnabledAndConString.CalledConstructorWithConnectionString.ShouldBe(true);
            contextAppWithConfigEnabledAndConString.Database.Connection.ConnectionString.ShouldBe("Server=localhost;Database=TenantContext;User=sa;Password=123");
            
            LocalIocManager.Register<IAbpSession, MyChangableSession>();
            LocalIocManager.Resolve<MyChangableSession>().TenantId = 1;
            LocalIocManager.Resolve<MyChangableSession>().UserId = 1;
            LocalIocManager.Register<ISettingManager, MockSettingManager>();
            if (LocalIocManager.IsRegistered<ISettingManager>())
            {
                var setManager = LocalIocManager.Resolve<ISettingManager>();
                setManager.ChangeSettingForTenant(1, MultiTenancySettingNames.TenantConnectionString, "Server=localhost;Database=SpecificTenantContext;User=sa;Password=123");
            }
                
            var contextAppConnectionStringTenantSetting = LocalIocManager.Resolve<MyAppDbContext>();
            contextAppConnectionStringTenantSetting.CalledConstructorWithConnectionString.ShouldBe(true);
            contextAppConnectionStringTenantSetting.Database.Connection.ConnectionString.ShouldBe("Server=localhost;Database=SpecificTenantContext;User=sa;Password=123");
            
        }

        public class MyAppDbContext : AbpDbContext, IMultiTenancySplitContext
        {
            public bool CalledConstructorWithConnectionString { get; private set; }

            public MyAppDbContext()
            {

            }

            public MyAppDbContext(string nameOrConnectionString)
                : base(nameOrConnectionString)
            {
                CalledConstructorWithConnectionString = true;
            }

            public override void Initialize()
            {

            }
        }

        public class MySecondDbContext : AbpDbContext
        {
            public bool CalledConstructorWithConnectionString { get; private set; }

            public MySecondDbContext()
            {

            }

            public MySecondDbContext(string nameOrConnectionString)
                : base(nameOrConnectionString)
            {
                CalledConstructorWithConnectionString = true;
            }

            public override void Initialize()
            {

            }
        }

        public class MyDbContext : AbpDbContext
        {
            public bool CalledConstructorWithConnectionString { get; private set; }

            public MyDbContext()
            {

            }

            public MyDbContext(string nameOrConnectionString)
                : base(nameOrConnectionString)
            {
                CalledConstructorWithConnectionString = true;
            }

            public override void Initialize()
            {

            }
        }
        

        public class MockSettingManager  : SettingManager
        {
            public MockSettingManager(IAbpSession abpSession)
                : base(CreateMockSettingDefinitionManager())
            {
                this.SettingStore = new MemorySettingStore();
                this.AbpSession = abpSession;
            }

        }
        private static ISettingDefinitionManager CreateMockSettingDefinitionManager()
        {
            var settings = new Dictionary<string, SettingDefinition>
            {                
                {MultiTenancySettingNames.TenantConnectionString, new SettingDefinition(MultiTenancySettingNames.TenantConnectionString, "", scopes: SettingScopes.Tenant)},
            };

            var definitionManager = Substitute.For<ISettingDefinitionManager>();

            //Implement methods
            definitionManager.GetSettingDefinition(Arg.Any<string>()).Returns(x => settings[x[0].ToString()]);
            definitionManager.GetAllSettingDefinitions().Returns(settings.Values.ToList());

            return definitionManager;
        }

        private class MemorySettingStore : ISettingStore
        {
            private readonly List<SettingInfo> _settings;

            public MemorySettingStore()
            {
                _settings = new List<SettingInfo>
                {
                    new SettingInfo(null, null, MultiTenancySettingNames.TenantConnectionString, "")
                };
            }

            public Task<SettingInfo> GetSettingOrNullAsync(int? tenantId, long? userId, string name)
            {
                return Task.FromResult(_settings.FirstOrDefault(s => s.TenantId == tenantId && s.UserId == userId && s.Name == name));
            }

            public async Task DeleteAsync(SettingInfo setting)
            {
                _settings.RemoveAll(s => s.TenantId == setting.TenantId && s.UserId == setting.UserId && s.Name == setting.Name);
            }

            public async Task CreateAsync(SettingInfo setting)
            {
                _settings.Add(setting);
            }

            public async Task UpdateAsync(SettingInfo setting)
            {
                var s = await GetSettingOrNullAsync(setting.TenantId, setting.UserId, setting.Name);
                if (s != null)
                {
                    s.Value = setting.Value;
                }
            }

            public Task<List<SettingInfo>> GetAllListAsync(int? tenantId, long? userId)
            {
                return Task.FromResult(_settings.Where(s => s.TenantId == tenantId && s.UserId == userId).ToList());
            }
        }

    }
}