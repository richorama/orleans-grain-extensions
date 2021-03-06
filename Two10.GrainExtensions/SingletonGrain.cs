﻿using Microsoft.WindowsAzure.ServiceRuntime;
using Orleans;
using System;
using System.Threading.Tasks;

namespace Two10.GrainExtensions
{

    /// <summary>
    /// A grain which uses a blob lease to ensure it is the only activation. This is at the sacrafice of availability
    /// </summary>
    public abstract class SingletonGrain : Grain
    {
        IDisposable timer;
        BlobLease lease;
        protected string ConnectionString { get; set; }
        protected string ContainerName { get; set; }

        SingletonGrain()
        {
            if (string.IsNullOrWhiteSpace(this.ConnectionString) && RoleEnvironment.IsAvailable)
            {
                // attempt to load data connection string out of role environment
                var connectionString = RoleEnvironment.GetConfigurationSettingValue("DataConnectionString");
                if (!string.IsNullOrWhiteSpace(connectionString)) this.ConnectionString = connectionString;
            }

            if (string.IsNullOrWhiteSpace(this.ContainerName))
            {
                this.ContainerName = "locks";
            }
        }

        public override async Task OnActivateAsync()
        {
            this.lease = new BlobLease(ConnectionString, ContainerName, this.IdentityString);

            // this method will throw if the lease cannot be aquired
            await this.lease.Init(TimeSpan.FromSeconds(30));
           
            // renew the lease with 10 seconds to spare 
            this.timer = this.RegisterTimer(this.Renew, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));

            await base.OnActivateAsync();
        }

        async Task Renew(object _ = null)
        { 
            try
            {
                await this.lease.Renew();
            }
            catch 
            {
                // lease renewal failed, kill the grain
                this.timer.Dispose();
                this.DeactivateOnIdle();
            }
        }

        public override async Task OnDeactivateAsync()
        {
            try
            {
                await this.lease.Release();
            }
            catch
            { }
            await base.OnDeactivateAsync();
        }

    }
}
