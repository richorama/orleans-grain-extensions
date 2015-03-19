using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        protected abstract string ConnectionString { get; }
        protected abstract string ContainerName { get; }


        public override async Task OnActivateAsync()
        {
            this.lease = new BlobLease(ConnectionString, ContainerName, this.IdentityString);

            // this method will throw if the lease cannot be aquired
            await this.lease.Init(TimeSpan.FromSeconds(30));
           
            // renew the lease with 10 seconds to spare 
            this.timer = this.RegisterTimer(this.Renew, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));

            await base.OnActivateAsync();
        }

        Task Renew(object _ = null)
        { 
            try
            {
                return this.lease.Renew();
            }
            catch 
            {
                // lease renewal failed, kill the grain
                this.timer.Dispose();
                this.DeactivateOnIdle();
                return TaskDone.Done;
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
