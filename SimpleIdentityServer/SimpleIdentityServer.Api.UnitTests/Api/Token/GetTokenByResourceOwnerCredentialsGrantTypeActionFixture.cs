﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using Moq;
using NUnit.Framework;
using SimpleIdentityServer.Core.Api.Token.Actions;
using SimpleIdentityServer.Core.Authenticate;
using SimpleIdentityServer.Core.Errors;
using SimpleIdentityServer.Core.Exceptions;
using SimpleIdentityServer.Core.Helpers;
using SimpleIdentityServer.Core.Jwt;
using SimpleIdentityServer.Core.JwtToken;
using SimpleIdentityServer.Core.Models;
using SimpleIdentityServer.Core.Parameters;
using SimpleIdentityServer.Core.Repositories;
using SimpleIdentityServer.Core.Validators;
using SimpleIdentityServer.Logging;

namespace SimpleIdentityServer.Api.UnitTests.Api.Token
{
    [TestFixture]
    public sealed class GetTokenByResourceOwnerCredentialsGrantTypeActionFixture
    {
        private Mock<IGrantedTokenRepository> _grantedTokenRepositoryFake;

        private Mock<IGrantedTokenGeneratorHelper> _grantedTokenGeneratorHelperFake;

        private Mock<IScopeValidator> _scopeValidatorFake;

        private Mock<IResourceOwnerValidator> _resourceOwnerValidatorFake;

        private Mock<ISimpleIdentityServerEventSource> _simpleIdentityServerEventSourceFake;

        private Mock<IAuthenticateClient> _authenticateClientFake;

        private Mock<IJwtGenerator> _jwtGeneratorFake;

        private IGetTokenByResourceOwnerCredentialsGrantTypeAction _getTokenByResourceOwnerCredentialsGrantTypeAction;

        [Test]
        public void When_Passing_No_Request_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT & ASSERT
            Assert.Throws<ArgumentNullException>(() => _getTokenByResourceOwnerCredentialsGrantTypeAction.Execute(null, null));
        }

        [Test]
        public void When_The_Client_Cannot_Be_Authenticated_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();
            const string clientAssertion = "clientAssertion";
            const string clientAssertionType = "clientAssertionType";
            const string clientId = "clientId";
            const string clientSecret = "clientSecret";
            var resourceOwnerGrantTypeParameter = new ResourceOwnerGrantTypeParameter
            {
                ClientAssertion = clientAssertion,
                ClientAssertionType = clientAssertionType,
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            string message;
            _authenticateClientFake.Setup(a => a.Authenticate(It.IsAny<AuthenticateInstruction>(), out message))
                .Returns(() => null);
            
            // ACT & ASSERTS
            var exception = Assert.Throws<IdentityServerException>(() => _getTokenByResourceOwnerCredentialsGrantTypeAction.Execute(resourceOwnerGrantTypeParameter, null));
            Assert.IsTrue(exception.Code == ErrorCodes.InvalidClient);
        }
        
        [Test]
        public void When_The_Resource_Owner_Is_Not_Valid_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();
            const string clientAssertion = "clientAssertion";
            const string clientAssertionType = "clientAssertionType";
            const string clientId = "clientId";
            const string clientSecret = "clientSecret";
            var resourceOwnerGrantTypeParameter = new ResourceOwnerGrantTypeParameter
            {
                ClientAssertion = clientAssertion,
                ClientAssertionType = clientAssertionType,
                ClientId = clientId,
                ClientSecret = clientSecret
            };
            var client = new Client();

            string message;
            _authenticateClientFake.Setup(a => a.Authenticate(It.IsAny<AuthenticateInstruction>(), out message))
                .Returns(() => client);
            _resourceOwnerValidatorFake.Setup(
                r => r.ValidateResourceOwnerCredentials(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => null);

            // ACT & ASSERTS
            var exception = Assert.Throws<IdentityServerException>(() => _getTokenByResourceOwnerCredentialsGrantTypeAction.Execute(resourceOwnerGrantTypeParameter, null));
            Assert.IsTrue(exception.Code == ErrorCodes.InvalidGrant);
            Assert.IsTrue(exception.Message == ErrorDescriptions.ResourceOwnerCredentialsAreNotValid);
        }

        [Test]
        public void When_Passing_A_Not_Allowed_Scopes_Then_Exception_Is_Throw()
        {
            // ARRANGE
            InitializeFakeObjects();
            const string clientAssertion = "clientAssertion";
            const string clientAssertionType = "clientAssertionType";
            const string clientId = "clientId";
            const string clientSecret = "clientSecret";
            const string invalidScope = "invalidScope";
            var resourceOwnerGrantTypeParameter = new ResourceOwnerGrantTypeParameter
            {
                ClientAssertion = clientAssertion,
                ClientAssertionType = clientAssertionType,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scope = invalidScope
            };
            var client = new Client();
            var resourceOwner = new ResourceOwner();

            string message;
            _authenticateClientFake.Setup(a => a.Authenticate(It.IsAny<AuthenticateInstruction>(), out message))
                .Returns(() => client);
            _resourceOwnerValidatorFake.Setup(
                r => r.ValidateResourceOwnerCredentials(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => resourceOwner);
            _scopeValidatorFake.Setup(s => s.IsScopesValid(It.IsAny<string>(), It.IsAny<Client>(), out message))
                .Returns(() => new List<string>());

            // ACT & ASSERTS
            var exception = Assert.Throws<IdentityServerException>(() => _getTokenByResourceOwnerCredentialsGrantTypeAction.Execute(resourceOwnerGrantTypeParameter, null));
            Assert.IsTrue(exception.Code == ErrorCodes.InvalidScope);
        }

        [Test]
        public void When_Requesting_An_AccessToken_For_An_Authenticated_User_Then_AccessToken_Is_Granted()
        {
            // ARRANGE
            InitializeFakeObjects();
            const string clientAssertion = "clientAssertion";
            const string clientAssertionType = "clientAssertionType";
            const string clientId = "clientId";
            const string clientSecret = "clientSecret";
            const string invalidScope = "invalidScope";
            const string accessToken = "accessToken";
            var resourceOwnerGrantTypeParameter = new ResourceOwnerGrantTypeParameter
            {
                ClientAssertion = clientAssertion,
                ClientAssertionType = clientAssertionType,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scope = invalidScope
            };
            var client = new Client
            {
                ClientId = clientId
            };
            var resourceOwner = new ResourceOwner();
            var userInformationJwsPayload = new JwsPayload();
            var grantedToken = new GrantedToken
            {
                AccessToken = accessToken
            };

            string message;
            _authenticateClientFake.Setup(a => a.Authenticate(It.IsAny<AuthenticateInstruction>(), out message))
                .Returns(() => client);
            _resourceOwnerValidatorFake.Setup(
                r => r.ValidateResourceOwnerCredentials(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => resourceOwner);
            _scopeValidatorFake.Setup(s => s.IsScopesValid(It.IsAny<string>(), It.IsAny<Client>(), out message))
                .Returns(() => new List<string> { invalidScope });
            _jwtGeneratorFake.Setup(
                j => j.GenerateUserInfoPayloadForScope(It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthorizationParameter>()))
                .Returns(() => userInformationJwsPayload);
            _grantedTokenGeneratorHelperFake.Setup(g => g.GenerateToken(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<JwsPayload>(),
                It.IsAny<JwsPayload>()))
                .Returns(() => grantedToken);

            // ACT
            _getTokenByResourceOwnerCredentialsGrantTypeAction.Execute(resourceOwnerGrantTypeParameter, null);

            // ASSERT
            _grantedTokenRepositoryFake.Verify(g => g.Insert(grantedToken));
            _simpleIdentityServerEventSourceFake.Verify(s => s.GrantAccessToClient(clientId, accessToken, invalidScope));
        }

        private void InitializeFakeObjects()
        {
            _grantedTokenRepositoryFake = new Mock<IGrantedTokenRepository>();
            _grantedTokenGeneratorHelperFake = new Mock<IGrantedTokenGeneratorHelper>();
            _scopeValidatorFake = new Mock<IScopeValidator>();
            _resourceOwnerValidatorFake = new Mock<IResourceOwnerValidator>();
            _simpleIdentityServerEventSourceFake = new Mock<ISimpleIdentityServerEventSource>();
            _authenticateClientFake = new Mock<IAuthenticateClient>();
            _jwtGeneratorFake = new Mock<IJwtGenerator>();

            _getTokenByResourceOwnerCredentialsGrantTypeAction = new GetTokenByResourceOwnerCredentialsGrantTypeAction(
                _grantedTokenRepositoryFake.Object,
                _grantedTokenGeneratorHelperFake.Object,
                _scopeValidatorFake.Object,
                _resourceOwnerValidatorFake.Object,
                _simpleIdentityServerEventSourceFake.Object,
                _authenticateClientFake.Object,
                _jwtGeneratorFake.Object);
        }
    }
}
