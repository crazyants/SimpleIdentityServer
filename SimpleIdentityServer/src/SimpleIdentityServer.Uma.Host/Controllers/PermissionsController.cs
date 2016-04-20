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

using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using SimpleIdentityServer.Uma.Core.Api.PermissionController;
using SimpleIdentityServer.Uma.Host.DTOs.Requests;
using SimpleIdentityServer.Uma.Host.DTOs.Responses;
using SimpleIdentityServer.Uma.Host.Extensions;
using System;
using System.Net;

namespace SimpleIdentityServer.Uma.Host.Controllers
{
    [Route(Constants.RouteValues.Permission)]
    public class PermissionsController : Controller
    {
        private readonly IPermissionControllerActions _permissionControllerActions;

        #region Constructor

        public PermissionsController(IPermissionControllerActions permissionControllerActions)
        {
            _permissionControllerActions = permissionControllerActions;
        }

        #endregion

        #region Public methods

        [HttpPost]
        [Authorize("UmaProtection")]
        public ActionResult PostPermission([FromBody] PostPermission postPermission)
        {
            if (postPermission == null)
            {
                throw new ArgumentNullException(nameof(postPermission));
            }

            var parameter = postPermission.ToParameter();
            var clientId = this.GetClientId();
            var ticketId = _permissionControllerActions.AddPermission(parameter, clientId);
            var result = new AddPermissionResponse
            {
                TicketId = ticketId
            };
            return new ObjectResult(result)
            {
                StatusCode = (int)HttpStatusCode.Created
            };
        }

        #endregion
    }
}