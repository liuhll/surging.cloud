﻿namespace Surging.Cloud.CPlatform.Configurations
{
    public class ProtocolPortOptions
    {
        public int MQTTPort { get; set; }
    
        public int? HttpPort { get; set; }

        public int WSPort { get; set; }

        public int UdpPort { get; set; }
    }
}
