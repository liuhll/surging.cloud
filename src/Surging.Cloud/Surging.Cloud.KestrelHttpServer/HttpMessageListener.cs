﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Surging.Cloud.CPlatform;
using Surging.Cloud.CPlatform.Configurations;
using Surging.Cloud.CPlatform.Exceptions;
using Surging.Cloud.CPlatform.Messages;
using Surging.Cloud.CPlatform.Routing;
using Surging.Cloud.CPlatform.Routing.Template;
using Surging.Cloud.CPlatform.Serialization;
using Surging.Cloud.CPlatform.Transport;
using Surging.Cloud.CPlatform.Transport.Implementation;
using Surging.Cloud.KestrelHttpServer.Filters;
using Surging.Cloud.KestrelHttpServer.Filters.Implementation;
using Surging.Cloud.KestrelHttpServer.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Surging.Cloud.KestrelHttpServer
{
    public abstract class HttpMessageListener : IMessageListener
    {
        public event ReceivedDelegate Received;
        private readonly ILogger<HttpMessageListener> _logger;
        private readonly ISerializer<string> _serializer;
        private event RequestDelegate Requested;
        private readonly IServiceRouteProvider _serviceRouteProvider;
        private readonly string[] _serviceKeys = {  "serviceKey", "servicekey"};

        public HttpMessageListener(ILogger<HttpMessageListener> logger, ISerializer<string> serializer, IServiceRouteProvider serviceRouteProvider)
        {
            _logger = logger;
            _serializer = serializer;
            _serviceRouteProvider = serviceRouteProvider;
        }

        public async Task OnReceived(IMessageSender sender, TransportMessage message)
        {
            if (Received == null)
                return;
            await Received(sender, message);
        }

        public async Task OnReceived(IMessageSender sender,string messageId, HttpContext context, IEnumerable<IActionFilter> actionFilters)
        {
            var serviceRoute = context.Items["route"] as ServiceRoute;

            var path = (context.Items["path"]
                ?? HttpUtility.UrlDecode(GetRoutePath(context.Request.Path.ToString()))) as string;
            path = AppConfig.MapRoutePathOptions.GetRoutePath(path, context.Request.Method);
            if (serviceRoute == null)
            {
                var route = await _serviceRouteProvider.GetRouteByPathOrRegexPath(path, context.Request.Method);
                if (route == null)
                {
                    throw new CPlatformException($"未能找到路径为{path}-{context.Request.Method}的路由信息", StatusCode.Http404EndpointStatusCode);
                }
                serviceRoute = route;
            }
            IDictionary<string, object> parameters = context.Request.Query.ToDictionary(p => p.Key, p => (object)p.Value.ToString());
            object serviceKey = null;
            foreach (var key in _serviceKeys)
            {
                parameters.Remove(key, out object value);
                if (value != null)
                {
                    serviceKey = value;
                    break;
                }
            }

            if (!serviceRoute.ServiceDescriptor.RoutePath.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                var @params = RouteTemplateSegmenter.Segment(serviceRoute.ServiceDescriptor.RoutePath, path);
                foreach (var param in @params)
                {
                    parameters.Add(param.Key, param.Value);
                }
            }

            var httpMessage = new HttpMessage
            {
                Parameters = parameters,
                RoutePath = serviceRoute.ServiceDescriptor.RoutePath,
                HttpMethod = context.Request.Method,
                ServiceKey = serviceKey?.ToString()
            };

            if (context.Request.HasFormContentType)
            {
                if (context.Request.ContentType == "multipart/form-data")
                {
                    var collection = await GetFormCollection(context.Request);
                    httpMessage.Parameters.Add("form", collection);
                }
                else 
                {
                    var formData = new Dictionary<string, object>();
                    foreach (var item in context.Request.Form.Keys) 
                    {
                        formData.Add(item, context.Request.Form[item]);
                    }
                    
                    httpMessage.Parameters.Add("form", formData);
                }
               
                if (!await OnActionExecuting(new ActionExecutingContext { Context = context, Route = serviceRoute, Message = httpMessage }, 
                    sender, messageId, actionFilters)) return;
                httpMessage.Attachments = GetHttpMessageAttachments(context);
                await Received(sender, new TransportMessage(messageId,httpMessage));
            }
            else
            {
                StreamReader streamReader = new StreamReader(context.Request.Body);
                var data = await streamReader.ReadToEndAsync();
                if (context.Request.Method != "GET")
                {
                    var bodyParams = _serializer.Deserialize<string, IDictionary<string, object>>(data) ?? new Dictionary<string, object>();
                    foreach (var param in bodyParams)
                        httpMessage.Parameters.Add(param.Key, param.Value);
                    if (!await OnActionExecuting(new ActionExecutingContext { Context = context, Route = serviceRoute, Message = httpMessage },
                       sender,  messageId, actionFilters)) return;
                    httpMessage.Attachments = GetHttpMessageAttachments(context);
                    await Received(sender, new TransportMessage(messageId,httpMessage));
                }
                else
                {
                    if (!await OnActionExecuting(new ActionExecutingContext { Context = context, Route = serviceRoute, Message = httpMessage }, 
                        sender, messageId, actionFilters)) return;
                    httpMessage.Attachments = GetHttpMessageAttachments(context);
                    await Received(sender, new TransportMessage(messageId,httpMessage));
                }
            }

            

            await OnActionExecuted(context, httpMessage, actionFilters);
        }

        private IDictionary<string, object> GetHttpMessageAttachments(HttpContext context)
        {
            
            if (context.User.Claims != null && context.User.Claims.Any())
            {
                foreach (var claims in context.User.Claims)
                {
                    RpcContext.GetContext().SetAttachment(claims.Type, claims.Value);
                }
            }
            return RpcContext.GetContext().GetContextParameters();
        }

        private void RemoveClaims(IDictionary<string, object> httpMessageAttachments,string claimType)
        {
            if (httpMessageAttachments.ContainsKey(claimType)) 
            {
                httpMessageAttachments.Remove(claimType);
            }
        }

        public async Task<bool> OnActionExecuting(ActionExecutingContext filterContext, IMessageSender sender, string messageId, IEnumerable<IActionFilter> filters)
        {
            foreach (var fiter in filters)
            { 
                await fiter.OnActionExecuting(filterContext); 
                if (filterContext.Result != null)
                {
                    await sender.SendAndFlushAsync(new TransportMessage(messageId,filterContext.Result));
                    return false;
                }
            }
            return true;
        }

        public async Task OnActionExecuted(HttpContext context, HttpMessage message, IEnumerable<IActionFilter> filters)
        {
            foreach (var fiter in filters)
            {
                var filterContext = new ActionExecutedContext()
                {
                    Context = context,
                    Message = message
                };
                await fiter.OnActionExecuted(filterContext);
            }
        }

        public async Task<bool> OnAuthorization(HttpContext context, HttpServerMessageSender sender,string messageId, IEnumerable<IAuthorizationFilter> filters)
        {
            foreach (var filter in filters)
            {
                var path = HttpUtility.UrlDecode(GetRoutePath(context.Request.Path.ToString()));
                path = AppConfig.MapRoutePathOptions.GetRoutePath(path,context.Request.Method);
                var serviceRoute = await _serviceRouteProvider.GetRouteByPathOrRegexPath(path,context.Request.Method);
                if (serviceRoute == null)
                {
                    throw new CPlatformException($"未能找到:{path}-{context.Request.Method}的路由信息", StatusCode.Http404EndpointStatusCode);
                }
                context.Items.Add("route", serviceRoute);
                var filterContext = new AuthorizationFilterContext
                {
                    Path = path,
                    Context = context,
                    Route = serviceRoute
                };
                await filter.OnAuthorization(filterContext);
                if (filterContext.Result != null)
                {
                    await sender.SendAndFlushAsync(new TransportMessage(messageId,filterContext.Result));
                    return false;
                }
            }
            return true;
        }

        public async Task<bool> OnException(HttpContext context, HttpServerMessageSender sender, string messageId, Exception exception, IEnumerable<IExceptionFilter> filters)
        {
            foreach (var filter in filters)
            {
                var path = HttpUtility.UrlDecode(GetRoutePath(context.Request.Path.ToString()));
                var filterContext = new ExceptionContext
                {
                    RoutePath = path,
                    Context = context,
                    Exception = exception
                };
                await filter.OnException(filterContext);
                if (filterContext.Result != null)
                {
                    await sender.SendAndFlushAsync(new TransportMessage(messageId, filterContext.Result));
                    return false;
                }
            }
            return true;
        }

        private async Task<HttpFormCollection> GetFormCollection(HttpRequest request)
        {
            var boundary = GetName("boundary=", request.ContentType); 
            var reader = new MultipartReader(boundary, request.Body);
            var collection = await GetMultipartForm(reader);
            var fileCollection = new HttpFormFileCollection();
            var fields = new Dictionary<string, StringValues>();
            foreach (var item in collection)
            {
                if (item.Value is HttpFormFileCollection)
                {
                    var itemCollection = item.Value as HttpFormFileCollection;
                    fileCollection.AddRange(itemCollection);
                }
                else
                {
                    var itemCollection = item.Value as Dictionary<string, StringValues>;
                    fields = fields.Concat(itemCollection).ToDictionary(k => k.Key, v => v.Value);

                }
            }
           return new HttpFormCollection(fields, fileCollection);
        }

        private async Task<IDictionary<string,object>> GetMultipartForm(MultipartReader reader)
        {
           var section = await reader.ReadNextSectionAsync();
            var collection = new Dictionary<string, object>();
            if (section != null)
            { 
                var name=GetName("name=",section.ContentDisposition);
                var fileName = GetName("filename=",section.ContentDisposition);
                var buffer = new MemoryStream();
                await section.Body.CopyToAsync(buffer);
                if(string.IsNullOrEmpty(fileName))
                {
                    var fields = new Dictionary<string, StringValues>();
                    StreamReader streamReader = new StreamReader(buffer);
                    fields.Add(name, new StringValues(UTF8Encoding.Default.GetString(buffer.GetBuffer(),0,(int)buffer.Length)));
                    collection.Add(name, fields);
                }
                else
                {
                    var fileCollection = new HttpFormFileCollection();
                    StreamReader streamReader = new StreamReader(buffer);
                    fileCollection.Add(new HttpFormFile(buffer.Length,name,fileName,buffer.GetBuffer()));
                    collection.Add(name, fileCollection);
                }
                var formCollection= await GetMultipartForm(reader);
                foreach(var item in formCollection)
                {
                    if (!collection.ContainsKey(item.Key))
                        collection.Add(item.Key,item.Value);
                }
            }
            return collection;
        }

        private string GetName(string type,string content)
        {
            var elements = content.Split(';');
            var element = elements.Where(entry => entry.Trim().StartsWith(type)).FirstOrDefault()?.Trim();
            var name = element?.Substring(type.Length);
            if (!string.IsNullOrEmpty(name) && name.Length >= 2 && name[0] == '"' && name[name.Length - 1] == '"')
            {
                name = name.Substring(1, name.Length - 2);
            }
            return name;
        }

        private string GetRoutePath(string path)
        {
            string routePath = "";
            var urlSpan = path.AsSpan();
            var len = urlSpan.IndexOf("?");
            if (urlSpan.LastIndexOf("/") == 0)
            {
                routePath = path;
            }
            else
            {
                if (len == -1)
                    routePath = urlSpan.TrimStart("/").ToString().ToLower();
                else
                    routePath = urlSpan.Slice(0, len).TrimStart("/").ToString().ToLower();
            }
            return routePath;
        }
    }
}
