namespace Abp.Configuration.Startup
{
    /// <summary>
    /// Used to configure multi-tenancy.
    /// </summary>
    internal class MultiTenancyConfig : IMultiTenancyConfig
    {
        /// <summary>
        /// Is multi-tenancy enabled?
        /// Default value: false.
        /// </summary>
        public bool IsEnabled { get; set; }

                /// <summary>
        /// Gets/sets default connection string for the tenant context used by ORM module.
        /// It can be name of a connection string in application's config file or can be full connection string.
        /// </summary>
        public string DefaultNameOrConnectionStringTenant { get; set; }
    }
}