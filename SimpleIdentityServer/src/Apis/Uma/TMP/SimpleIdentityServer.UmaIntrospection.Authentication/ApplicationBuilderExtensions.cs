﻿#region copyright
// Copyright 2015 Habart Thierry
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using System;

namespace SimpleIdentityServer.UmaIntrospection.Authentication
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseAuthenticationWithUmaIntrospection(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<UmaIntrospectionMiddleware<UmaIntrospectionOptions>>(app);
        }

        public static IApplicationBuilder UseAuthenticationWithUmaIntrospection(this IApplicationBuilder app, UmaIntrospectionOptions umaIntrospectionOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (umaIntrospectionOptions == null)
            {
                throw new ArgumentNullException(nameof(umaIntrospectionOptions));
            }

            return app.UseMiddleware<UmaIntrospectionMiddleware<UmaIntrospectionOptions>>(app, Options.Create(umaIntrospectionOptions));
        }
    }
}
