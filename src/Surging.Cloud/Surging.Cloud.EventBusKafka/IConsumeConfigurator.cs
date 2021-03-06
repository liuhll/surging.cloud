﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Surging.Cloud.EventBusKafka
{
   public interface IConsumeConfigurator
    {
        void Configure(List<Type> consumers);

        void Unconfigure(List<Type> consumers);
    }
}
