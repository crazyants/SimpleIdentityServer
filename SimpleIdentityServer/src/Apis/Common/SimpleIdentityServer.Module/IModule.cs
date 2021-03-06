﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace SimpleIdentityServer.Module
{
    public interface IModule
    {
        void ConfigureServices(IServiceCollection services, IMvcBuilder mvcBuilder = null, IHostingEnvironment env = null, IDictionary<string, string> options = null, IEnumerable<ModuleUIDescriptor> moduleUiDescriptors = null);
        void ConfigureAuthorization(AuthorizationOptions authorizationOptions, IDictionary<string, string> options = null);
        void ConfigureAuthentication(AuthenticationBuilder authBuilder, IDictionary<string, string> options = null);
        void Configure(IApplicationBuilder applicationBuilder);
        void Configure(IRouteBuilder routeBuilder);
        IEnumerable<string> GetOptionKeys();
        ModuleUIDescriptor GetModuleUI();
    }
}
