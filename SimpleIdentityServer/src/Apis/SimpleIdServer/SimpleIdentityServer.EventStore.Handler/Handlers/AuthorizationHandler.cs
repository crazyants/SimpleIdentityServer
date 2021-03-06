﻿#region copyright
// Copyright 2017 Habart Thierry
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

using SimpleBus.Core;
using SimpleIdentityServer.EventStore.Core.Models;
using SimpleIdentityServer.EventStore.Core.Repositories;
using SimpleIdentityServer.Handler.Events;
using System;
using System.Threading.Tasks;

namespace SimpleIdentityServer.EventStore.Handler.Handlers
{
    public class AuthorizationHandler : IHandle<AuthorizationRequestReceived>, IHandle<AuthorizationGranted>, IHandle<ResourceOwnerAuthenticated>, IHandle<ConsentAccepted>, IHandle<ConsentRejected>
    {
        private readonly IEventAggregateRepository _repository;
        private readonly EventStoreHandlerOptions _options;

        public AuthorizationHandler(IEventAggregateRepository repository, EventStoreHandlerOptions options)
        {
            _repository = repository;
            _options = options;
        }

        public async Task Handle(AuthorizationRequestReceived message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            
            await _repository.Add(new EventAggregate
            {
                Id = message.Id,
                AggregateId = message.ProcessId,
                Description = "Start authorization process",
                CreatedOn = DateTime.UtcNow,
                Payload = message.Payload,
                Order = message.Order,
                Type = _options.Type,
                Verbosity = EventVerbosities.Information,
                Key = "auth_process_started"
            });
        }

        public async Task Handle(AuthorizationGranted message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            await _repository.Add(new EventAggregate
            {
                Id = message.Id,
                AggregateId = message.ProcessId,
                Description = "Authorization granted",
                CreatedOn = DateTime.UtcNow,
                Payload = message.Payload,
                Order = message.Order,
                Type = _options.Type,
                Verbosity = EventVerbosities.Information,
                Key = "auth_granted"
            });
        }

        public async Task Handle(ResourceOwnerAuthenticated message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            await _repository.Add(new EventAggregate
            {
                Id = message.Id,
                AggregateId = message.AggregateId,
                Description = "Resource owner is authenticated",
                CreatedOn = DateTime.UtcNow,
                Payload = message.Payload,
                Order = message.Order,
                Type = _options.Type,
                Verbosity = EventVerbosities.Information,
                Key = "resource_owner_auth"
            });
        }

        public async Task Handle(ConsentAccepted message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            
            await _repository.Add(new EventAggregate
            {
                Id = message.Id,
                AggregateId = message.ProcessId,
                Description = "Consent accepted",
                CreatedOn = DateTime.UtcNow,
                Payload = message.Payload,
                Order = message.Order,
                Type = _options.Type,
                Verbosity = EventVerbosities.Information,
                Key = "consent_accepted"
            });
        }

        public async Task Handle(ConsentRejected message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            await _repository.Add(new EventAggregate
            {
                Id = message.Id,
                AggregateId = message.ProcessId,
                Description = "Consent rejected",
                CreatedOn = DateTime.UtcNow,
                Order = message.Order,
                Type = _options.Type,
                Verbosity = EventVerbosities.Information,
                Key = "consent_rejected"
            });
        }
    }
}
