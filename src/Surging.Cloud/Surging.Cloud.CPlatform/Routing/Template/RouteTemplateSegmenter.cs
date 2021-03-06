﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Surging.Cloud.CPlatform.Routing.Template
{
   public class RouteTemplateSegmenter
    {
        public static Dictionary<string,object> Segment(string routePath, string path)
        {
            var pattern = "/{.*?}";
            var result = new Dictionary<string, object>();
            if ( Regex.IsMatch(routePath, pattern,RegexOptions.IgnoreCase))
            {
                var routeTemplate= Regex.Replace(routePath, pattern, "", RegexOptions.IgnoreCase);
                var routeSegments = routeTemplate.Split('/');
                var pathSegments = path.Split('/');
                var segments = routePath.Split("/");
                for(var i= routeSegments.Length;i< pathSegments.Length;i++)
                {
                    result.Add(segments[i].Replace("{","").Replace("}", ""), pathSegments[i]);
                }
            }
            return result;
        }
    }
}
