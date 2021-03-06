﻿using Microsoft.Extensions.Configuration;
using Surging.Cloud.CPlatform.Configurations.Remote;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Surging.Cloud.ApiGateWay.OAuth.Implementation.Configurations
{
   public class GatewayConfigurationProvider : FileConfigurationProvider
    {
        public GatewayConfigurationProvider(GatewayConfigurationSource source) : base(source) { }

        public override void Load(Stream stream)
        {
            var parser = new JsonConfigurationParser();
            this.Data = parser.Parse(stream, null);
        }
    }
}