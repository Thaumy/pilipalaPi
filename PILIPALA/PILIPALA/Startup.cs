using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PILIPALA.Models;
using WaterLibrary.pilipala.Components;
using WaterLibrary.pilipala;
using WaterLibrary.MySQL;
using WaterLibrary.pilipala.Database;
using PILIPALA.system.Theme;

namespace PILIPALA
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            //���ü�
            var AppSettings = Configuration.GetSection("AppSettings");//���ڵ�
            var DB_Connection = AppSettings.GetSection("Database:Connection");//���ݿ�������Ϣ�ڵ�
            var DB_Meta = AppSettings.GetSection("Database:Meta");//���ݿ�Ԫ��Ϣ�ڵ�
            var ThemeSection = AppSettings.GetSection("Theme");//������Ϣ�ڵ�
            var UserSection = AppSettings.GetSection("User");//�û���Ϣ�ڵ�
            //MySQL��������ʼ��
            var MySqlManager = new MySqlManager(new(
                DB_Connection.GetSection("DataSource").Value,
                Convert.ToInt32(DB_Connection.GetSection("Port").Value),
                DB_Connection.GetSection("User").Value,
                DB_Connection.GetSection("PWD").Value
            ), DB_Meta.GetSection("Name").Value);
            //�û�ģ��ע��
            services.AddTransient(x => new UserModel()
            {
                Account = UserSection.GetSection("Account").Value,
                PWD = UserSection.GetSection("PWD").Value
            });
            //���������ע��
            services.AddTransient(x => new ThemeHandler(new ThemeConfigModel()
            {
                Path = ThemeSection.GetSection("Path").Value,
            }));

            var PLDatabase = new PLDatabase
            {
                Tables = new
                (
                    DB_Meta.GetSection("Tables:User").Value,
                    DB_Meta.GetSection("Tables:Index").Value,
                    DB_Meta.GetSection("Tables:Backup").Value,
                    DB_Meta.GetSection("Tables:Archive").Value,
                    DB_Meta.GetSection("Tables:Comment").Value
                ),

                ViewsSet = new
                (
                    new(
                        DB_Meta.GetSection("ViewsSet:CleanViews:PosUnion").Value,
                        DB_Meta.GetSection("ViewsSet:CleanViews:NegUnion").Value
                        ),
                    new(
                        DB_Meta.GetSection("ViewsSet:DirtyViews:PosUnion").Value,
                        DB_Meta.GetSection("ViewsSet:DIrtyViews:NegUnion").Value
                        )
                ),
                MySqlManager = MySqlManager
            };
            services.AddTransient<ICORE>(x => new CORE(PLDatabase));

            services.AddCors(options =>
            {
                options.AddPolicy("DefaultPolicy",
                    builder =>
                    {
                        builder.AllowAnyOrigin();
                    });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseDeveloperExceptionPage();
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseCors();
            app.UseSession();

            /* �û�API */
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "UserAPI",
                    pattern: "user/{action}",
                    defaults: new { controller = "User" });
            });
            /* �ÿ�API */
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "GuestAPI",
                    pattern: "guest/{action}",
                    defaults: new { controller = "Guest" });
            });
            /* Pannel·�� */
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "List",
                    pattern: "",
                    defaults: new { controller = "Panel", action = "List", ajax = false });
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "Content",
                    pattern: "{ID}",
                    defaults: new { controller = "Panel", action = "Content", ajax = false });
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "@List",
                    pattern: "@/-1",
                    defaults: new { controller = "Panel", action = "List", ajax = true });
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "@Content",
                    pattern: "@/{ID}",
                    defaults: new { controller = "Panel", action = "Content", ajax = true });
            });
        }
    }
}