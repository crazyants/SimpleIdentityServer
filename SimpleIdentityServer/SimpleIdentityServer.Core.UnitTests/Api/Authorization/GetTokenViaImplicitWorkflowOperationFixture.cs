﻿using System;
using System.Security.Claims;
using Moq;
using NUnit.Framework;
using SimpleIdentityServer.Core.Api.Authorization.Actions;
using SimpleIdentityServer.Core.Api.Authorization.Common;
using SimpleIdentityServer.Core.Common;
using SimpleIdentityServer.Core.Errors;
using SimpleIdentityServer.Core.Exceptions;
using SimpleIdentityServer.Core.Parameters;
using SimpleIdentityServer.Core.Results;
using SimpleIdentityServer.Logging;

namespace SimpleIdentityServer.Core.UnitTests.Api.Authorization
{
    [TestFixture]
    public class GetTokenViaImplicitWorkflowOperationFixture
    {
        private Mock<IProcessAuthorizationRequest> _processAuthorizationRequestFake;

        private Mock<IGenerateAuthorizationResponse> _generateAuthorizationResponseFake;

        private Mock<ISimpleIdentityServerEventSource> _simpleIdentityServerEventSourceFake;

        private IGetTokenViaImplicitWorkflowOperation _getTokenViaImplicitWorkflowOperation;

        [Test]
        public void When_Passing_No_Authorization_Request_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();
            
            // ACT & ASSERT
            Assert.Throws<ArgumentNullException>(() => _getTokenViaImplicitWorkflowOperation.Execute(null, null));
        }

        [Test]
        public void When_Passing_No_Nonce_Parameter_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();
            var authorizationParameter = new AuthorizationParameter
            {
                State = "state"
            };

            // ACT & ASSERTS
            var exception = Assert.Throws<IdentityServerExceptionWithState>(() => _getTokenViaImplicitWorkflowOperation.Execute(authorizationParameter, null));
            Assert.IsTrue(exception.Code == ErrorCodes.InvalidRequestCode);
            Assert.IsTrue(exception.Message == string.Format(ErrorDescriptions.MissingParameter, Constants.StandardAuthorizationRequestParameterNames.NonceName));
            Assert.IsTrue(exception.State == authorizationParameter.State);
        }

        [Test]
        public void When_Requesting_Authorization_With_Valid_Request_Then_Events_Are_Logged()
        {
            // ARRANGE
            InitializeFakeObjects();
            const string clientId = "clientId";
            const string scope = "openid";
            var authorizationParameter = new AuthorizationParameter
            {
                State = "state",
                Nonce =  "nonce",
                ClientId =  clientId,
                Scope = scope,
                Claims = null
            };
            var actionResult = new ActionResult()
            {
                Type = TypeActionResult.RedirectToAction,
                RedirectInstruction = new RedirectInstruction
                {
                    Action = IdentityServerEndPoints.ConsentIndex
                }
            };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity("fake"));
            _processAuthorizationRequestFake.Setup(p => p.Process(It.IsAny<AuthorizationParameter>(),
                It.IsAny<ClaimsPrincipal>())).Returns(actionResult);

            // ACT
            var result = _getTokenViaImplicitWorkflowOperation.Execute(authorizationParameter, claimsPrincipal);

            // ASSERTS
            _simpleIdentityServerEventSourceFake.Verify(s => s.StartImplicitFlow(clientId, scope, string.Empty));
            _simpleIdentityServerEventSourceFake.Verify(s => s.EndImplicitFlow(clientId, "RedirectToAction", "ConsentIndex"));
        }

        private void InitializeFakeObjects()
        {
            _processAuthorizationRequestFake = new Mock<IProcessAuthorizationRequest>();
            _generateAuthorizationResponseFake = new Mock<IGenerateAuthorizationResponse>();
            _simpleIdentityServerEventSourceFake = new Mock<ISimpleIdentityServerEventSource>();
            _getTokenViaImplicitWorkflowOperation = new GetTokenViaImplicitWorkflowOperation(
                _processAuthorizationRequestFake.Object,
                _generateAuthorizationResponseFake.Object,
                _simpleIdentityServerEventSourceFake.Object);
        }
    }
}
