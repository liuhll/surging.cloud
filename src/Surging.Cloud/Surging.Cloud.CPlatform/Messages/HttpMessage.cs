﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Surging.Cloud.CPlatform.Messages
{
    public  class HttpMessage
    { 
        public string RoutePath { get; set; }

        public string HttpMethod { get; set; }

        public string ServiceKey { get; set; } 

        public IDictionary<string,object> Parameters { get; set; }

        public IDictionary<string, object> Attachments { get; set; }
    }
}
