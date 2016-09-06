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

using Newtonsoft.Json;
using SimpleIdentityServer.Uma.Core.Errors;
using SimpleIdentityServer.Uma.Core.Exceptions;
using SimpleIdentityServer.Uma.Core.Helpers;
using SimpleIdentityServer.Uma.Core.Models;
using SimpleIdentityServer.Uma.Core.Parameters;
using SimpleIdentityServer.Uma.Core.Policies;
using SimpleIdentityServer.Uma.Core.Repositories;
using SimpleIdentityServer.Uma.Core.Responses;
using SimpleIdentityServer.Uma.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Uma.Core.Api.Authorization.Actions
{
    public interface IGetAuthorizationAction
    {
        Task<AuthorizationResponse> Execute(
               GetAuthorizationActionParameter getAuthorizationActionParameter,
               IEnumerable<System.Security.Claims.Claim> claims);
    }

    internal class GetAuthorizationAction : IGetAuthorizationAction
    {
        private readonly ITicketRepository _ticketRepository;

        private readonly IAuthorizationPolicyValidator _authorizationPolicyValidator;

        private readonly UmaServerOptions _umaServerOptions;

        private readonly IRptRepository _rptRepository;

        private readonly IRepositoryExceptionHelper _repositoryExceptionHelper;

        private readonly IUmaServerEventSource _umaServerEventSource;

        #region Constructor

        public GetAuthorizationAction(
            ITicketRepository ticketRepository,
            IAuthorizationPolicyValidator authorizationPolicyValidator,
            UmaServerOptions umaServerOptions,
            IRptRepository rptRepository,
            IRepositoryExceptionHelper repositoryExceptionHelper,
            IUmaServerEventSource umaServerEventSource)
        {
            _ticketRepository = ticketRepository;
            _authorizationPolicyValidator = authorizationPolicyValidator;
            _umaServerOptions = umaServerOptions;
            _rptRepository = rptRepository;
            _repositoryExceptionHelper = repositoryExceptionHelper;
            _umaServerEventSource = umaServerEventSource;
        }

        #endregion

        #region Public methods

        public async Task<AuthorizationResponse> Execute(
            GetAuthorizationActionParameter getAuthorizationActionParameter,
            IEnumerable<System.Security.Claims.Claim> claims)
        {
            var json = getAuthorizationActionParameter == null ? string.Empty : JsonConvert.SerializeObject(getAuthorizationActionParameter);
            _umaServerEventSource.StartGettingAuthorization(json);
            if (getAuthorizationActionParameter == null)
            {
                throw new ArgumentNullException(nameof(getAuthorizationActionParameter));
            }

            if (claims == null ||
                !claims.Any())
            {
                throw new ArgumentNullException(nameof(claims));
            }

            if (string.IsNullOrWhiteSpace(getAuthorizationActionParameter.TicketId))
            {
                throw new BaseUmaException(ErrorCodes.InvalidRequestCode,
                    string.Format(ErrorDescriptions.TheParameterNeedsToBeSpecified, "ticket_id"));
            }

            var ticket = _ticketRepository.GetTicketById(getAuthorizationActionParameter.TicketId);
            if (ticket == null)
            {
                throw new BaseUmaException(ErrorCodes.InvalidTicket,
                    string.Format(ErrorDescriptions.TheTicketDoesntExist, getAuthorizationActionParameter.TicketId));
            }

            var clientId = GetClientId(claims);
            if (ticket.ClientId != clientId)
            {
                throw new BaseUmaException(ErrorCodes.InvalidTicket,
                    ErrorDescriptions.TheTicketIssuerIsDifferentFromTheClient);
            }

            if (ticket.ExpirationDateTime < DateTime.UtcNow)
            {
                throw new BaseUmaException(ErrorCodes.ExpiredTicket,
                    ErrorDescriptions.TheTicketIsExpired);
            }

            _umaServerEventSource.CheckAuthorizationPolicy(json);
            var authorizationResult = await _authorizationPolicyValidator.IsAuthorized(ticket,
                clientId,
                getAuthorizationActionParameter.ClaimTokenParameters);
            if (authorizationResult.Type != AuthorizationPolicyResultEnum.Authorized)
            {
                _umaServerEventSource.RequestIsNotAuthorized(json);
                return new AuthorizationResponse
                {
                    AuthorizationPolicyResult = authorizationResult.Type,
                    ErrorDetails = authorizationResult.ErrorDetails
                };
            }

            var rpt = new Rpt
            {
                ExpirationDateTime = DateTime.UtcNow.AddSeconds(_umaServerOptions.RptLifeTime),
                Value = Guid.NewGuid().ToString(),
                TicketId = ticket.Id,
                ResourceSetId = ticket.ResourceSetId,
                CreateDateTime = DateTime.UtcNow
            };

            _repositoryExceptionHelper.HandleException(
                ErrorDescriptions.TheRptCannotBeInserted,
                () => _rptRepository.InsertRpt(rpt));
            _umaServerEventSource.RequestIsAuthorized(json);
            return new AuthorizationResponse
            {
                AuthorizationPolicyResult = AuthorizationPolicyResultEnum.Authorized,
                Rpt = rpt.Value
            };
        }

        #endregion

        #region Private static methods

        private static string GetClientId(IEnumerable<System.Security.Claims.Claim> claims)
        {
            var clientClaim = claims.FirstOrDefault(c => c.Type == "client_id");
            if (clientClaim == null)
            {
                return string.Empty;
            }

            return clientClaim.Value;
        }

        #endregion
    }
}
