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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SimpleIdentityServer.Client;
using SimpleIdentityServer.Client.Introspection;
using SimpleIdentityServer.Uma.Common;
using SimpleIdentityServer.Uma.Common.DTOs;
using SimpleIdentityServer.UmaManager.Client;
using SimpleIdentityServer.UmaManager.Client.DTOs.Requests;
using SimpleIdentityServer.UmaManager.Client.DTOs.Responses;
using SimpleIdentityServer.UmaManager.Client.Resources;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SimpleIdentityServer.UmaIntrospection.Authentication.Tests
{
    public class UmaIntrospectionMiddlewareFixture
    {
        private class FakeStartup
        {
            public void ConfigureServices(IServiceCollection serviceCollection)
            {
                serviceCollection.AddMvc();
            }

            public void Configure(IApplicationBuilder app)
            {
                var umaIntrospectionOptions = app.ApplicationServices.GetRequiredService<UmaIntrospectionOptions>();
                app.UseAuthenticationWithUmaIntrospection(umaIntrospectionOptions);
                app.Map("/operation", a =>
                {
                    a.Run(async context =>
                    {
                        var user = context.User;
                        var claimsIdentity = user.Identity as ClaimsIdentity;
                        var permissions = claimsIdentity.GetPermissions();
                        context.Response.StatusCode = 200;
                    });
                });
            }
        }

        [Fact]
        public void When_Passing_No_Option_Then_Exception_Is_Thrown()
        {
            // ACT & ASSERT
            Assert.Throws<ArgumentNullException>(() => CreateServer(null));
        }

        [Fact]
        public async Task When_Passing_Invalid_ResourceUrl_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            var umaIntrospectionOptions = new UmaIntrospectionOptions
            {
                UmaConfigurationUrl = "http://localhost",
                ResourcesUrl = "invalid_url"
            };
            var createServer = CreateServer(umaIntrospectionOptions);
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("http://localhost/operation")
            };
            httpRequestMessage.Headers.Add("Authorization", "Bearer RPT");
            var client = createServer.CreateClient();

            // ACT & ASSERTS
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await client.SendAsync(httpRequestMessage)).ConfigureAwait(false);
            Assert.True(exception.Message == $"url {umaIntrospectionOptions.ResourcesUrl} is not well formatted");
        }

        [Fact]
        public async Task When_Permissions_Are_Returned_Then_Claims_Are_Added()
        {
            // ARRANGE
            var introspectionResponse = new IntrospectionResponse
            {
                IsActive = true,
                Permissions = new List<PermissionResponse>
                {
                    new PermissionResponse
                    {
                        ResourceSetId = "resource_set_id",
                        Scopes = new List<string>
                        {
                            "execute"
                        }
                    }
                }
            };
            var searchOperationResponse = new List<ResourceResponse>
            {
                new ResourceResponse
                {
                    ResourceSetId = "resource_set_id"
                }
                // ApplicationName = "application_name",
                // OperationName = "operation_name"
            };
            var stubIdentityServerUmaClientFactory = new Mock<IIdentityServerUmaClientFactory>();
            var stubIntrospectionClient = new Mock<IIntrospectionClient>();
            stubIntrospectionClient
                .Setup(s => s.GetByResolution(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(introspectionResponse);
            stubIdentityServerUmaClientFactory
                .Setup(s => s.GetIntrospectionClient())
                .Returns(() => stubIntrospectionClient.Object);
            var stubIdentityServerUmaManagerClientFactory = new Mock<IIdentityServerUmaManagerClientFactory>();
            var stubOperationClient = new Mock<IResourceClient>();
            stubOperationClient.Setup(s => s.SearchResources(It.IsAny<SearchResourceRequest>(), It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(searchOperationResponse);
            stubIdentityServerUmaManagerClientFactory.Setup(s => s.GetResourceClient())
                .Returns(stubOperationClient.Object);
            var umaIntrospectionOptions = new UmaIntrospectionOptions
            {
                UmaConfigurationUrl = "http://localhost/uma",
                ResourcesUrl = "http://localhost/resources",
                IdentityServerUmaManagerClientFactory = stubIdentityServerUmaManagerClientFactory.Object,
                IdentityServerUmaClientFactory = stubIdentityServerUmaClientFactory.Object
            };
            var createServer = CreateServer(umaIntrospectionOptions);
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("http://localhost/operation")
            };
            httpRequestMessage.Headers.Add("Authorization", "Bearer RPT");
            var client = createServer.CreateClient();

            // ACT
            var result = await client.SendAsync(httpRequestMessage).ConfigureAwait(false);

            // ASSERT
            Assert.True(result.StatusCode == HttpStatusCode.OK);
        }

        private static TestServer CreateServer(UmaIntrospectionOptions umaIntrospectionOptions)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices((services) =>
                {
                    InitializeServices(services, umaIntrospectionOptions);
                })
                .UseStartup(typeof(FakeStartup));
            return new TestServer(builder);
        }

        private static void InitializeServices(IServiceCollection services, UmaIntrospectionOptions options)
        {
            services.AddSingleton(options);
        }
    }
}
