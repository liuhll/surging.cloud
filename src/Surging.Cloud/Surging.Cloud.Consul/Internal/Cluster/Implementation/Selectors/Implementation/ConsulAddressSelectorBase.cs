﻿using Surging.Cloud.CPlatform.Address;
using Surging.Cloud.CPlatform.Runtime.Client.Address.Resolvers.Implementation.Selectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Surging.Cloud.Consul.Internal.Cluster.Implementation.Selectors.Implementation
{
    public abstract class ConsulAddressSelectorBase : IConsulAddressSelector
    {
        #region Implementation of IAddressSelector

        /// <summary>
        /// 选择一个地址。
        /// </summary>
        /// <param name="context">地址选择上下文。</param>
        /// <returns>地址模型。</returns>
        async Task<AddressModel> IAddressSelector.SelectAsync(AddressSelectContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Descriptor == null)
                throw new ArgumentNullException(nameof(context.Descriptor));
            if (context.Address == null)
                throw new ArgumentNullException(nameof(context.Address));

            if (!context.Address.Any())
                throw new ArgumentException("没有任何地址信息。", nameof(context.Address)); 

            if (context.Address.Count() == 1)
            {
                return context.Address.First();
            }
            else
            {
                return await SelectAsync(context);
            }
        }

        #endregion Implementation of IAddressSelector

        /// <summary>
        /// 选择一个地址。
        /// </summary>
        /// <param name="context">地址选择上下文。</param>
        /// <returns>地址模型。</returns>
        protected abstract ValueTask<AddressModel> SelectAsync(AddressSelectContext context);
    }
}