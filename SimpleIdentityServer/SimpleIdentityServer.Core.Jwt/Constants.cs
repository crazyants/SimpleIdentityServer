﻿using System.Collections.Generic;
using System.Security.Claims;

namespace SimpleIdentityServer.Core.Jwt
{
    public class Constants
    {
        public static class StandardClaimNames
        {
            public static string Issuer = "iss";

            public static string Audiences = "aud";

            public static string ExpirationTime = "exp";

            public static string Iat = "iat";

            public static string AuthenticationTime = "auth_time";

            public static string Nonce = "nonce";

            public static string Acr = "acr";

            public static string Amr = "amr";

            public static string Azp = "azp";

            /// <summary>
            /// Unique identifier of the JWT.
            /// </summary>
            public static string Jti = "jti";
        }

        public static class StandardResourceOwnerClaimNames
        {
            public static string Subject = "sub";

            public static string Name = "name";

            public static string GivenName = "given_name";

            public static string FamilyName = "family_name";

            public static string MiddleName = "middle_name";

            public static string NickName = "nickname";

            public static string PreferredUserName = "preferred_username";

            public static string Profile = "profile";

            public static string Picture = "picture";

            public static string WebSite = "website";

            public static string Email = "email";

            public static string EmailVerified = "email_verified";

            public static string Gender = "gender";

            public static string BirthDate = "birthdate";

            public static string ZoneInfo = "zoneinfo";

            public static string Locale = "locale";

            public static string PhoneNumber = "phone_number";

            public static string PhoneNumberVerified = "phone_number_verified";

            public static string Address = "address";

            public static string UpdatedAt = "updated_at";

            // Check where this claims is defined.
            public static string Role = "role";
        }

        public static readonly Dictionary<string, string> MapWifClaimsToOpenIdClaims = new Dictionary<string, string>
        {
            {
                ClaimTypes.Name, StandardResourceOwnerClaimNames.Name
            },
            {
                ClaimTypes.GivenName, StandardResourceOwnerClaimNames.GivenName
            },
            {
                ClaimTypes.Webpage, StandardResourceOwnerClaimNames.WebSite
            },
            {
                ClaimTypes.Email, StandardResourceOwnerClaimNames.Email
            },
            {
                ClaimTypes.Gender, StandardResourceOwnerClaimNames.Gender
            },
            {
                ClaimTypes.DateOfBirth, StandardResourceOwnerClaimNames.BirthDate
            },
            {
                ClaimTypes.Locality, StandardResourceOwnerClaimNames.Locale
            },
            {
                ClaimTypes.HomePhone, StandardResourceOwnerClaimNames.PhoneNumber
            },
            {
                ClaimTypes.MobilePhone, StandardResourceOwnerClaimNames.PhoneNumberVerified
            },
            {
                ClaimTypes.StreetAddress, StandardResourceOwnerClaimNames.Address
            },
            {
                ClaimTypes.Role, StandardResourceOwnerClaimNames.Role
            }
        };
    }
}
