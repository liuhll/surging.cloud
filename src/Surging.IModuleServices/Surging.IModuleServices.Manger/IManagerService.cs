﻿

using Surging.Cloud.CPlatform.Ioc;
using Surging.Cloud.CPlatform.Runtime.Client.Address.Resolvers.Implementation.Selectors.Implementation;
using Surging.Cloud.CPlatform.Runtime.Server.Implementation.ServiceDiscovery.Attributes;
using Surging.Cloud.CPlatform.Support;
using Surging.Cloud.CPlatform.Support.Attributes;
using System.Threading.Tasks;

namespace Surging.IModuleServices.User
{

    [ServiceBundle("api/{Service}")]
    public interface IManagerService : IServiceKey
    {
        [Command(Strategy = StrategyType.Injection, ShuntStrategy = AddressSelectorMode.HashAlgorithm, ExecutionTimeoutInMilliseconds = 2500, BreakerRequestVolumeThreshold = 3, Injection = @"return 1;", RequestCacheEnabled = false)]
        Task<string> SayHello(string name);
    }
}
