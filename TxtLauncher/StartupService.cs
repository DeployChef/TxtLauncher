using System;
using Microsoft.Extensions.DependencyInjection;

namespace TxtLauncher
{
    public class StartupService
    {
        public void Configure(IServiceCollection services)
        {
            
        }

        public IServiceProvider BuildProvider(IServiceCollection services)
        {
            return services.BuildServiceProvider();
        }
    }
}
