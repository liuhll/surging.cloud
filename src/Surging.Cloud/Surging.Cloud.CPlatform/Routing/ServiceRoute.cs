﻿using Surging.Cloud.CPlatform.Address;
using Surging.Cloud.CPlatform.Serialization;
using Surging.Cloud.CPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Surging.Cloud.CPlatform.Routing
{
    /// <summary>
    /// 服务路由。
    /// </summary>
    public class ServiceRoute 
    {
        /// <summary>
        /// 服务可用地址。
        /// </summary>
        public ICollection<AddressModel> Address { get; set; }
        /// <summary>
        /// 服务描述符。
        /// </summary>
        public ServiceDescriptor ServiceDescriptor { get; set; }

        public ServiceRoute Copy()
        {
            var serviceRoute = new ServiceRoute();
            serviceRoute.ServiceDescriptor = ServiceDescriptor.DeepCopy<ServiceDescriptor>();
            var copyAddresses = new List<AddressModel>();
            foreach (var a in Address) 
            {
                var copyAddress = a.DeepCopy<IpAddressModel>();
                copyAddresses.Add(copyAddress);
            }
            serviceRoute.Address = copyAddresses;
            return serviceRoute;
        }

        #region Equality members

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            var model = obj as ServiceRoute;
            if (model == null)
                return false;

            if (obj.GetType() != GetType())
                return false;

            if (model.ServiceDescriptor != ServiceDescriptor)
                return false;

            return model.Address.Count() == Address.Count() && model.Address.All(addressModel => Address.Contains(addressModel));
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public static bool operator ==(ServiceRoute model1, ServiceRoute model2)
        {
            return Equals(model1, model2);
        }

        public static bool operator !=(ServiceRoute model1, ServiceRoute model2)
        {
            return !Equals(model1, model2);
        }

        #endregion Equality members
    }
}