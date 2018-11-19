using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Hangfire.JobKits;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

namespace CoreSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {

            services.AddHangfire(config =>
            {
                config.UseConsole();
                config.UseMemoryStorage();
                config.UseJobKits(typeof(Startup).Assembly);

            });
            services.AddMvc();

            var autofacContainer = services.BuildAutofacContainer();

            return autofacContainer.UseServiceProvider();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseHangfireServer();

            app.UseHangfireDashboard("", new DashboardOptions
            {
                Authorization = new List<IDashboardAuthorizationFilter>()
            });
            
        }
    }


    public static class AutofacConfig
    {
        /// <summary>
        /// ���� Autofac �̪ۨ`�J�A�ȱ��f
        /// </summary>
        /// <param name="services">�A�ȫظm����</param>
        /// <returns></returns>
        public static IContainer BuildAutofacContainer(this IServiceCollection services)
        {
            var builder = new ContainerBuilder();

            // ���U�̪ۨ`�J����
            var registerAssemblies = AssemblyHelper.GetAssemblies("HangfireWeb");

            foreach (var assembly in registerAssemblies)
            {
                builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            }

            builder.Populate(services);

            return builder.Build();
        }

        /// <summary>
        /// ���o�̪ۨ`�J�A�ȱ��f
        /// </summary>
        /// <param name="container">Autofac Container</param>
        /// <returns></returns>
        public static IServiceProvider UseServiceProvider(this IContainer container)
        {
            return new AutofacServiceProvider(container);
        }
    }

    public class AssemblyHelper
    {
        /// <summary>
        /// ���o������ Assembly
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <returns></returns>
        public static IEnumerable<Assembly> GetAssemblies(string assemblyName)
        {
            return GetAssemblies(assemblyName, new string[] { });
        }

        /// <summary>
        /// ���o������ Assembly 
        /// </summary>
        /// <param name="assemblyName">Assembly �W��</param>
        /// <param name="excludedName">�ư��W��</param>
        /// <returns></returns>
        public static IEnumerable<Assembly> GetAssemblies(string assemblyName, string[] excludedName)
        {
            var dependencies = DependencyContext.Default.RuntimeLibraries
                                                .Where(lib =>
                                                       lib.Name == assemblyName ||
                                                       lib.Name.StartsWith(assemblyName, System.StringComparison.Ordinal));

            foreach (var library in dependencies)
            {
                if (excludedName.Any(e => string.Compare(e, library.Name, true) == 0)) continue;

                yield return Assembly.Load(new AssemblyName(library.Name));
            }

        }
    }

}