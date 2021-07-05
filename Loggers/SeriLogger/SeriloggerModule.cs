using System;
using System.Reflection;
using Abp.Modules;
using Microsoft.Extensions.Configuration;

namespace SeriLogger
{
    public class SeriLoggerModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }

        public override void PreInitialize()
        {
            base.PreInitialize();
        }

        public override void PostInitialize()
        {
            base.PostInitialize();
        }
    }
}
