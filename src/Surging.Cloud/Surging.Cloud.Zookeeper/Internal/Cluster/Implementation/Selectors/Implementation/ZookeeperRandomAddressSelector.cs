﻿using Surging.Cloud.CPlatform.Address;
using Surging.Cloud.CPlatform.Runtime.Client.Address.Resolvers.Implementation.Selectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Surging.Cloud.Zookeeper.Internal.Cluster.Implementation.Selectors.Implementation
{
    public  class ZookeeperRandomAddressSelector : ZookeeperAddressSelectorBase
    {
        #region Field

        private readonly Func<int, int, int> _generate;
        private readonly Random _random;

        #endregion Field

        #region Constructor

        /// <summary>
        /// 初始化一个以Random生成随机数的随机地址选择器。
        /// </summary>
        public ZookeeperRandomAddressSelector()
        {
            _random = new Random();
            _generate = (min, max) => _random.Next(min, max);
        }

        /// <summary>
        /// 初始化一个自定义的随机地址选择器。
        /// </summary>
        /// <param name="generate">随机数生成委托，第一个参数为最小值，第二个参数为最大值（不可以超过该值）。</param>
        public ZookeeperRandomAddressSelector(Func<int, int, int> generate)
        {
            if (generate == null)
                throw new ArgumentNullException(nameof(generate));
            _generate = generate;
        }

        #endregion Constructor

        #region Overrides of AddressSelectorBase

        /// <summary>
        /// 选择一个地址。
        /// </summary>
        /// <param name="context">地址选择上下文。</param>
        /// <returns>地址模型。</returns>
        protected override async Task<AddressModel> SelectAsync(AddressSelectContext context)
        {
            var address = context.Address.ToArray();
            var length = address.Length;

            var index = _generate(0, length);
            return address[index];
        }

        protected override async Task<string> SelectConnAsync(AddressSelectContext context) 
        {
            var conns = context.Connections.ToArray();
            var length = conns.Length;

            var index = _generate(0, length);
            return conns[index];
        }

        #endregion Overrides of AddressSelectorBase
    }
}

