﻿namespace Microsoft.ApplicationInsights.Wcf.Tests
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.ServiceModel.Description;

    internal class ContractBuilder
    {
        public static ContractDescription CreateDescription(Type contractType, Type serviceType)
        {
            var assemblyName = typeof(ContractDescription).Assembly.FullName;
            var typeLoaderType = Type.GetType("System.ServiceModel.Description.TypeLoader," + assemblyName);

            object typeLoader = Activator.CreateInstance(typeLoaderType);
            return (ContractDescription)typeLoaderType.InvokeMember(
                "LoadContractDescription",
                BindingFlags.InvokeMethod,
                null,
                typeLoader,
                new object[] { contractType, serviceType },
                CultureInfo.InvariantCulture);
        }
    }
}
