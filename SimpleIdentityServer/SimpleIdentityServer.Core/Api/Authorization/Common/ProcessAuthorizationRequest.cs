﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using SimpleIdentityServer.Core.Errors;
using SimpleIdentityServer.Core.Exceptions;
using SimpleIdentityServer.Core.Extensions;
using SimpleIdentityServer.Core.Factories;
using SimpleIdentityServer.Core.Helpers;
using SimpleIdentityServer.Core.Models;
using SimpleIdentityServer.Core.Parameters;
using SimpleIdentityServer.Core.Results;
using SimpleIdentityServer.Core.Validators;

namespace SimpleIdentityServer.Core.Api.Authorization.Common
{
    public interface IProcessAuthorizationRequest
    {
        ActionResult Process(
            AuthorizationParameter authorizationParameter,
            IPrincipal claimsPrincipal,
            string code);
    }

    public class ProcessAuthorizationRequest : IProcessAuthorizationRequest
    {
        private readonly IParameterParserHelper _parameterParserHelper;

        private readonly IClientValidator _clientValidator;

        private readonly IScopeValidator _scopeValidator;

        private readonly IActionResultFactory _actionResultFactory;

        private readonly IConsentHelper _consentHelper;

        public ProcessAuthorizationRequest(
            IParameterParserHelper parameterParserHelper,
            IClientValidator clientValidator,
            IScopeValidator scopeValidator,
            IActionResultFactory actionResultFactory,
            IConsentHelper consentHelper)
        {
            _parameterParserHelper = parameterParserHelper;
            _clientValidator = clientValidator;
            _scopeValidator = scopeValidator;
            _actionResultFactory = actionResultFactory;
            _consentHelper = consentHelper;
        }

        public ActionResult Process(
            AuthorizationParameter authorizationParameter, 
            IPrincipal claimsPrincipal, 
            string code)
        {
            if (authorizationParameter == null)
            {
                throw new ArgumentNullException("authorization parameter may not be null");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException("code parameter may not be null");
            }

            var prompts = _parameterParserHelper.ParsePromptParameters(authorizationParameter.Prompt);
            if (prompts == null || !prompts.Any())
            {
                prompts = new List<PromptParameter>();
                var endUserIsAuthenticated = IsAuthenticated(claimsPrincipal);
                if (!endUserIsAuthenticated)
                {
                    prompts.Add(PromptParameter.login);
                }
                else
                {
                    var confirmedConsent = GetResourceOwnerConsent(
                        claimsPrincipal,
                        authorizationParameter);
                    if (confirmedConsent == null)
                    {
                        prompts.Add(PromptParameter.consent);
                    }
                    else
                    {
                        prompts.Add(PromptParameter.none);
                    }
                }
            }

            var client = _clientValidator.ValidateClientExist(authorizationParameter.ClientId);
            if (client == null)
            {
                throw new IdentityServerExceptionWithState(
                    ErrorCodes.InvalidClient,
                    string.Format(ErrorDescriptions.ClientIsNotValid, authorizationParameter.ClientId),
                    authorizationParameter.State);
            }

            var redirectionUrl = _clientValidator.ValidateRedirectionUrl(authorizationParameter.RedirectUrl, client);
            if (string.IsNullOrWhiteSpace(redirectionUrl))
            {
                throw new IdentityServerExceptionWithState(
                    ErrorCodes.InvalidRequestCode,
                    string.Format(ErrorDescriptions.RedirectUrlIsNotValid, authorizationParameter.RedirectUrl),
                    authorizationParameter.State);
            }

            string messageError;
            var allowedScopes = _scopeValidator.IsScopesValid(authorizationParameter.Scope, client, out messageError);
            if (!allowedScopes.Any())
            {
                throw new IdentityServerExceptionWithState(
                    ErrorCodes.InvalidScope,
                    messageError,
                    authorizationParameter.State);
            }

            if (!allowedScopes.Contains(Constants.StandardScopes.OpenId.Name))
            {
                throw new IdentityServerExceptionWithState(
                    ErrorCodes.InvalidScope,
                    string.Format(ErrorDescriptions.TheScopesNeedToBeSpecified, Constants.StandardScopes.OpenId.Name),
                    authorizationParameter.State);
            }

            var responseTypes = _parameterParserHelper.ParseResponseType(authorizationParameter.ResponseType);
            if (!responseTypes.Any())
            {
                throw new IdentityServerExceptionWithState(
                    ErrorCodes.InvalidRequestCode,
                    string.Format(ErrorDescriptions.MissingParameter, Constants.StandardAuthorizationRequestParameterNames.ResponseTypeName),
                    authorizationParameter.State);
            }

            if (!_clientValidator.ValidateResponseTypes(responseTypes, client))
            {
                throw new IdentityServerExceptionWithState(
                    ErrorCodes.InvalidRequestCode,
                    string.Format(ErrorDescriptions.TheClientDoesntSupportTheResponseType,
                        authorizationParameter.ClientId,
                        string.Join(",", responseTypes)),
                    authorizationParameter.State);
            }

            var result = ProcessPromptParameters(
                prompts,
                claimsPrincipal,
                authorizationParameter,
                code);

            return result;
        }

        /// <summary>
        /// Process the prompt authorizationParameter.
        /// </summary>
        /// <param name="prompts">Prompt authorizationParameter values</param>
        /// <param name="principal">User's claims</param>
        /// <param name="authorizationParameter">Authorization code grant-type authorizationParameter</param>
        /// <param name="code">Encrypted and signed authorization code grant-type authorizationParameter</param>
        /// <returns>The action result interpreted by the controller</returns>
        private ActionResult ProcessPromptParameters(
            IList<PromptParameter> prompts,
            IPrincipal principal,
            AuthorizationParameter authorizationParameter,
            string code)
        {
            var endUserIsAuthenticated = IsAuthenticated(principal);

            // Raise "login_required" exception : if the prompt authorizationParameter is "none" AND the user is not authenticated
            // Raise "interaction_required" exception : if there's no consent from the user.
            if (prompts.Contains(PromptParameter.none))
            {
                if (!endUserIsAuthenticated)
                {
                    throw new IdentityServerExceptionWithState(
                        ErrorCodes.LoginRequiredCode,
                        ErrorDescriptions.TheUserNeedsToBeAuthenticated,
                        authorizationParameter.State);
                }

                var confirmedConsent = GetResourceOwnerConsent(principal, authorizationParameter);
                if (confirmedConsent == null)
                {
                    throw new IdentityServerExceptionWithState(
                            ErrorCodes.InteractionRequiredCode,
                            ErrorDescriptions.TheUserNeedsToGiveIsConsent,
                            authorizationParameter.State);
                }

                var result = _actionResultFactory.CreateAnEmptyActionResultWithRedirectionToCallBackUrl();
                return result;
            }

            // Check if the user is still valid.
            if (endUserIsAuthenticated && 
                !authorizationParameter.MaxAge.Equals(default(double)))
            {
                var claimsPrincipal = principal as ClaimsPrincipal;
                if (claimsPrincipal == null)
                {
                    throw new IdentityServerExceptionWithState(
                        ErrorCodes.LoginRequiredCode,
                        ErrorDescriptions.TheClaimCannotBeFetch,
                        authorizationParameter.State);
                }

                var authenticationDateTimeClaim =
                    claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.AuthenticationInstant);
                if (authenticationDateTimeClaim != null)
                {
                    var maxAge = authorizationParameter.MaxAge;
                    var currentDateTimeUtc = DateTimeOffset.UtcNow.ConvertToUnixTimestamp();
                    var authenticationDateTime = long.Parse(authenticationDateTimeClaim.Value);
                    if (maxAge < currentDateTimeUtc - authenticationDateTime)
                    {
                        var result = _actionResultFactory.CreateAnEmptyActionResultWithRedirection();
                        result.RedirectInstruction.AddParameter("code", code);
                        result.RedirectInstruction.Action = IdentityServerEndPoints.AuthenticateIndex;
                        return result;
                    }
                }
            }

            // Redirects to the authentication screen 
            // if the "prompt" authorizationParameter is equal to "login" OR
            // The user is not authenticated AND the prompt authorizationParameter is different from "none"
            if (prompts.Contains(PromptParameter.login))
            {
                var result = _actionResultFactory.CreateAnEmptyActionResultWithRedirection();
                result.RedirectInstruction.AddParameter("code", code);
                result.RedirectInstruction.Action = IdentityServerEndPoints.AuthenticateIndex;
                return result;
            }

            if (prompts.Contains(PromptParameter.consent))
            {
                var result = _actionResultFactory.CreateAnEmptyActionResultWithRedirection();
                result.RedirectInstruction.AddParameter("code", code);
                if (!endUserIsAuthenticated)
                {
                    result.RedirectInstruction.Action = IdentityServerEndPoints.AuthenticateIndex;
                    return result;
                }

                result.RedirectInstruction.Action = IdentityServerEndPoints.ConsentIndex;
                return result;
            }

            return null;
        }

        private Consent GetResourceOwnerConsent(
            IPrincipal claimsPrincipal,
            AuthorizationParameter authorizationParameter)
        {
            var principal = claimsPrincipal as ClaimsPrincipal;
            var subject = principal.GetSubject();
            return _consentHelper.GetConsentConfirmedByResourceOwner(subject, authorizationParameter);
        }

        private static bool IsAuthenticated(IPrincipal principal)
        {
            return principal == null || principal.Identity == null ?
                false :
                principal.Identity.IsAuthenticated;
        }
    }
}
