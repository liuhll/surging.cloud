﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Surging.Cloud.ApiGateWay;
using Surging.Cloud.ApiGateWay.Configurations;
using Surging.Cloud.ApiGateWay.OAuth.Implementation.Configurations;
using Surging.Cloud.Caching.Configurations;
using Surging.Cloud.Codec.MessagePack;
using Surging.Cloud.Consul;
using Surging.Cloud.Consul.Configurations;
using Surging.Cloud.CPlatform;
using Surging.Cloud.CPlatform.Utilities;
using Surging.Cloud.DotNetty;
using Surging.Cloud.ProxyGenerator;
using Surging.Cloud.Zookeeper;
//using Surging.Cloud.Zookeeper;
using ZookeeperConfigInfo =  Surging.Cloud.Zookeeper.Configurations.ConfigInfo;
using System;
using ApiGateWayConfig = Surging.Cloud.ApiGateWay.AppConfig;
using Surging.Cloud.Caching;
using Surging.Cloud.CPlatform.Cache;
using System.Linq;

namespace Surging.ApiGateway
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public IContainer ApplicationContainer { get; private set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(env.ContentRootPath)
              .AddCacheFile("Configs/cacheSettings.json", optional: false)
              .AddJsonFile("Configs/appsettings.json", optional: true, reloadOnChange: true)
              .AddGatewayFile("Configs/gatewaySettings.json", optional: false)
              .AddJsonFile($"Configs/appsettings.{env.EnvironmentName}.json", optional: true);
            Configuration = builder.Build();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return RegisterAutofac(services);
        }

        private IServiceProvider RegisterAutofac(IServiceCollection services)
        {
            var registerConfig = ApiGateWayConfig.Register;
            services.AddMvc(options => {
                options.Filters.Add(typeof(CustomExceptionFilterAttribute));
            }).AddJsonOptions(options => {
                //options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });
            services.AddLogging();
            services.AddCors();
            var builder = new ContainerBuilder();
            builder.Populate(services); 
            builder.AddMicroService(option =>
            {
                option.AddClient();
                option.AddCache();
                //option.UseZooKeeperManager(new ConfigInfo("127.0.0.1:2181"));
               if(registerConfig.Provider== RegisterProvider.Consul)
                option.UseConsulManager(new ConfigInfo(registerConfig.Address,enableChildrenMonitor:false));
               else if(registerConfig.Provider == RegisterProvider.Zookeeper)
                    option.UseZooKeeperManager(new ZookeeperConfigInfo(registerConfig.Address, enableChildrenMonitor: true));
                option.UseDotNettyTransport();
                option.AddApiGateWay();
                option.AddFilter(new ServiceExceptionFilter());
                //option.UseProtoBufferCodec();
                option.UseMessagePackCodec();
                builder.Register(m => new CPlatformContainer(ServiceLocator.Current));
            });
            ServiceLocator.Current = builder.Build();
            return new AutofacServiceProvider(ServiceLocator.Current);

        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //loggerFactory.AddConsole();
            var serviceCacheProvider = ServiceLocator.Current.Resolve<ICacheNodeProvider>();
            var addressDescriptors = serviceCacheProvider.GetServiceCaches().ToList();
            ServiceLocator.Current.Resolve<IServiceProxyFactory>();
            ServiceLocator.Current.Resolve<IServiceCacheManager>().SetCachesAsync(addressDescriptors);
            ServiceLocator.Current.Resolve<IConfigurationWatchProvider>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseCors(builder =>
            {
                var policy = Core.ApiGateWay.AppConfig.Policy;
                builder.WithOrigins(policy.Origins);
                if (policy.AllowAnyHeader)
                    builder.AllowAnyHeader();
                if (policy.AllowAnyMethod)
                    builder.AllowAnyMethod();
                if (policy.AllowAnyOrigin)
                    builder.AllowAnyOrigin();
                if (policy.AllowCredentials)
                    builder.AllowCredentials();
            });
            var myProvider = new FileExtensionContentTypeProvider();
            myProvider.Mappings.Add(".tpl", "text/plain");
            app.UseStaticFiles(new StaticFileOptions() { ContentTypeProvider = myProvider });
            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapRoute(
                "Path",
                "{*path}",
                new { controller = "Services", action = "Path" });
            });
        }
    }
}