# Experimental Extension to Orleans Grains

## Singleton Grain

Orleans does not _guarantee_ a single instance of a grain. Instead it favours availability. Therefore during a network partition, multiple activations of the same grain ID may be present in the system.

The `SingletonGrain` base class attempts to address this, by taking out a lease on an Azure Blob to limit the number of activations to no more than one. This is at the cost of availability, an increased network traffic to the blob storage system. It also slows down activation of the grain. 

If the lease is taken by another grain, or the lease taken be a grain lost in failure (up to 30 seconds by default), then the grain will throw an exception in the `ActivateAsync` method, and fail to activate. You will see an exception.

## Usage

```cs
/// <summary>
/// Implementation of a Singleton Grain
/// </summary>
public class Grain1 : SingletonGrain, IGrain1
{
    // you must implement a property which returns the storage connection string
    protected override string ConnectionString 
    {
        get { return "UseDevelopmentStorage=true"; } 
    }

    // you must implement a property which returns the container name to use for the blob locks
    protected override string ContainerName
    {
        get { return "bloblockcontainer"; }
    }

    // grain methods can be added as usual
    public Task<string> Hello()
    {
        return Task.FromResult(this.GetPrimaryKeyString());
    }
}
```

## How it works

Under the covers, when you activate the grain it creates a blob using the grain's identity as the name. It then attempts to acquire a lock on the blob.

If this attempt fails, the `ActiveAsync` method fails, and the grain is not created.

If the lock is acquired, the grain maintains the lock every 20 seconds using a timer.

# License

MIT