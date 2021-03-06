﻿using Surging.Cloud.CPlatform.Routing;
using Surging.Cloud.CPlatform.Runtime.Server;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Surging.Cloud.CPlatform.Routing
{
    /// <summary>
    /// 服务路由接口
    /// </summary>
    public interface IServiceRouteProvider
    {
        /// <summary>
        /// 根据服务id找到相关服务信息
        /// </summary>
        /// <param name="serviceId"></param>
        /// <returns></returns>
        Task<ServiceRoute> Locate(string serviceId,bool fromCache = true);

        Task<ServiceRoute> GetRouteByPathOrRegexPath(string path,string httpMethod);

        /// <summary>
        /// 根据服务路由路径找到相关服务信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<ServiceRoute> SearchRoute(string path, string httpMethod);

        /// <summary>
        /// 注册路由
        /// </summary>
        /// <param name="processorTime"></param>
        /// <returns></returns>
        Task RegisterRoutes(double processorTime);

        Task RemoveHostAddress(string serviceId);

        //Task RegisterRoutes(IEnumerable<ServiceEntry> serviceEntries);

        void UpdateServiceRouteCache(ServiceRoute serviceRoute);

    }
}
