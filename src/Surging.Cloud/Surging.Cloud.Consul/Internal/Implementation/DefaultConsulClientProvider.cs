﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Logging;
using Surging.Cloud.Consul.Configurations;
using Surging.Cloud.Consul.Internal.Cluster.HealthChecks;
using Surging.Cloud.Consul.Internal.Cluster.Implementation.Selectors;
using Surging.Cloud.Consul.Internal.Cluster.Implementation.Selectors.Implementation;
using Surging.Cloud.CPlatform;
using Surging.Cloud.CPlatform.Address;
using Surging.Cloud.CPlatform.Exceptions;
using Surging.Cloud.CPlatform.Runtime.Client.Address.Resolvers.Implementation.Selectors;
using Level = Microsoft.Extensions.Logging.LogLevel;

namespace Surging.Cloud.Consul.Internal.Implementation
{
    public class DefaultConsulClientProvider : IConsulClientProvider
    {
        private ConfigInfo _config;
        private readonly IHealthCheckService _healthCheckService;
        private readonly IConsulAddressSelector _consulAddressSelector;
        private readonly ILogger<DefaultConsulClientProvider> _logger;
        private readonly ConcurrentDictionary<string, IAddressSelector> _addressSelectors = new
            ConcurrentDictionary<string, IAddressSelector>();
        private readonly ConcurrentDictionary<AddressModel, ConsulClient> _consulClients = new
            ConcurrentDictionary<AddressModel, ConsulClient>();

        public DefaultConsulClientProvider(ConfigInfo config, IHealthCheckService healthCheckService, IConsulAddressSelector consulAddressSelector,
            ILogger<DefaultConsulClientProvider> logger)
        {
            _config = config;
            _healthCheckService = healthCheckService;
            _consulAddressSelector = consulAddressSelector;
            _logger = logger;
        }

        public async Task<ConsulClient> GetClient()
        {
            ConsulClient result = null;
            var address = new List<AddressModel>();
            foreach (var addressModel in _config.Addresses)
            {
                await _healthCheckService.Monitor(addressModel);
                var isHealth = await _healthCheckService.IsHealth(addressModel);
                if (!isHealth)
                {
                    _logger.LogWarning($"服务注册中心地址{addressModel.ToString()}不健康。");
                    continue;
                }
                address.Add(addressModel);
            }
            if (!address.Any())
            {
                if (_logger.IsEnabled(Level.Warning))
                    _logger.LogWarning($"找不到可用的注册中心地址。");
                return null;
            }

            var addr = await _consulAddressSelector.SelectAsync(new AddressSelectContext
            {
                Descriptor = new ServiceDescriptor { Id = nameof(DefaultConsulClientProvider) },
                Address = address
            });

            if (addr != null)
            {
                var ipAddress = addr as IpAddressModel;
                result = _consulClients.GetOrAdd(ipAddress, new ConsulClient(config =>
                  {
                      config.Address = new Uri($"http://{ipAddress.Ip}:{ipAddress.Port}");
                  }, null, h => { h.UseProxy = false; h.Proxy = null; }));

                

            }
            return result;
        }

        public async Task<IEnumerable<ConsulClient>> GetClients()
        {
            var result = new List<ConsulClient>();
            foreach (var address in _config.Addresses)
            {
                var ipAddress = address as IpAddressModel;
                if (await _healthCheckService.IsHealth(address))
                {
                    result.Add(_consulClients.GetOrAdd(ipAddress, new ConsulClient(config =>
                    {
                        config.Address = new Uri($"http://{ipAddress.Ip}:{ipAddress.Port}");
                    }, null, h => { h.UseProxy = false; h.Proxy = null; })));

                }
            }           
            return result;
        }

        //public async Task Check()
        //{
        //    foreach (var address in _config.Addresses)
        //    {
        //        if (!await _healthCheckService.IsHealth(address))
        //        {
        //            throw new RegisterConnectionException(string.Format("注册中心{0}连接异常，请联系管理员", address.ToString()));
        //        }
        //    }
        //}
    }
}
