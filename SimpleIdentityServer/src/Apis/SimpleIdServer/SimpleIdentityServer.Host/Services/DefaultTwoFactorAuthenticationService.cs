﻿using SimpleIdentityServer.Core.Common.Models;
using SimpleIdentityServer.Core.Services;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Host.Services
{
    public class DefaultTwoFactorAuthenticationService : ITwoFactorAuthenticationService
    {
        public int Code => throw new System.NotImplementedException();

        public Task SendAsync(string code, ResourceOwner user)
        {
            return Task.FromResult(0);
        }
    }
}
