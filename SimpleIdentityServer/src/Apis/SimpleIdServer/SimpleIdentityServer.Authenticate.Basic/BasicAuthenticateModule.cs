﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleIdentityServer.Module;
using System;
using System.Collections.Generic;

namespace SimpleIdentityServer.Authenticate.Basic
{
    public class BasicAuthenticateModule : IModule
    {
        public static ModuleUIDescriptor ModuleUi = new ModuleUIDescriptor
        {
            Title = "Authenticate",
            RelativeUrl = "~/Authenticate",
            IsAuthenticated = false,
            Picture = "~/img/Unknown.png"
        };

        public void ConfigureServices(IServiceCollection services, IMvcBuilder mvcBuilder = null, IHostingEnvironment env = null, IDictionary<string, string> options = null, IEnumerable<ModuleUIDescriptor> moduleUIDescriptors = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (mvcBuilder == null)
            {
                throw new ArgumentNullException(nameof(mvcBuilder));
            }

            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            services.AddBasicAuthentication(mvcBuilder, env);
        }

        public void Configure(IApplicationBuilder applicationBuilder)
        {
        }

        public void Configure(IRouteBuilder routeBuilder)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }

            routeBuilder.UseUserPasswordAuthentication();
        }

        public void ConfigureAuthorization(AuthorizationOptions authorizationOptions, IDictionary<string, string> options = null)
        {
        }

        public void ConfigureAuthentication(AuthenticationBuilder authBuilder, IDictionary<string, string> options = null)
        {
        }

        public IEnumerable<string> GetOptionKeys()
        {
            return new string[0];
        }

        public ModuleUIDescriptor GetModuleUI()
        {
            return ModuleUi;
        }
    }
}
