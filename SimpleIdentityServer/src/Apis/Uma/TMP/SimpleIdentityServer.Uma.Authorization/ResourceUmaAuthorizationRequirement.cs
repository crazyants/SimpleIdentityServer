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

using Microsoft.AspNetCore.Authorization;
using SimpleIdentityServer.Uma.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Uma.Authorization
{
    public class ResourceUmaAuthorizationRequirement : AuthorizationHandler<ResourceUmaAuthorizationRequirement>, IAuthorizationRequirement
    {
        private readonly string _url;

        private readonly List<string> _scopes;

        #region Constructor

        public ResourceUmaAuthorizationRequirement(string url, List<string> scopes)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (scopes == null || !scopes.Any())
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            _url = url;
            _scopes = scopes;
        }

        #endregion

        #region Protected methods

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ResourceUmaAuthorizationRequirement requirement)
        {
            if (context.User == null)
            {
                return Task.FromResult(0);
            }

            var claimsIdentity = context.User.Identity as ClaimsIdentity;
            if (claimsIdentity == null)
            {
                return Task.FromResult(0);
            }

            var permissions = claimsIdentity.GetPermissions();
            if (permissions.Any(p => string.Equals(p.Url, _url, StringComparison.OrdinalIgnoreCase) &&
                _scopes.All(s => p.Scopes.Any(sc => string.Equals(sc, s, StringComparison.OrdinalIgnoreCase)))))
            {
                context.Succeed(requirement);
            }

            return Task.FromResult(0);
        }

        #endregion
    }
}
