// <copyright file="Startup.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductService.Api.GRPCServices;

namespace ProductService.Api
{
    /// <summary>
    /// 启动配置.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup" /> class.
        /// </summary>
        /// <param name="configuration">配置.</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Gets 配置.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">服务集合.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
            services.AddControllers().AddDapr();
           
            services.AddDbContextPool<ProductContext>(options => { options.UseMySql(Configuration.GetConnectionString("MysqlConnection")); });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">app.</param>
        /// <param name="env">env.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            
            app.UseRouting();

            app.UseCloudEvents();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {   
                endpoints.MapSubscribeHandler();
                endpoints.MapControllers();
                endpoints.MapGrpcService<DaprClientService>();
            });
        }
    }
}