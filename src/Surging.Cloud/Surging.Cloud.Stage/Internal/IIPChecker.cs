﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Surging.Cloud.Stage.Internal
{
   public interface IIPChecker
    {
        bool IsBlackIp(IPAddress ip, string routePath);
    }
}
