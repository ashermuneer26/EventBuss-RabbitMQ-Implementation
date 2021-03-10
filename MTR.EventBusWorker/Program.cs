using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using BaseModule.Configurations;
using Microsoft.Extensions.Configuration;

namespace MTR.EventBusWorker
{
    class Program
    {
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        static async Task Main(string[] args)
        {
            Console.CancelKeyPress += (sender, eArgs) => {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };

            using (var bootstrapper = AbpBootstrapper.Create<EventBusWorkerModule>())
            {
                bootstrapper.Initialize();

                using (var queueListener = bootstrapper.IocManager.ResolveAsDisposable<QueueListener>())
                {
                    queueListener.Object.Start();
                    Console.WriteLine("Ctrl + C to Quit");
                    _quitEvent.WaitOne();
                    queueListener.Object.Stop();
                }
            }
        }
    }
}
