﻿using System;
using System.Linq;

using SimpleIdentityServer.Core.Common.Extensions;
using SimpleIdentityServer.Core.Jwt.Extensions;

namespace SimpleIdentityServer.Core.Jwt.Encrypt
{
    public interface IJweGenerator
    {
        string GenerateJwe(
            string entry,
            JweAlg alg,
            JweEnc enc,
            JsonWebKey jsonWebKey);
    }

    public class JweGenerator : IJweGenerator
    {
        private readonly IJweHelper _jweHelper;

        public JweGenerator(IJweHelper jweHelper)
        {
            _jweHelper = jweHelper;
        }

        public string GenerateJwe(
            string entry,
            JweAlg alg,
            JweEnc enc,
            JsonWebKey jsonWebKey)
        {
            var algo = Constants.MappingNameToJweAlgEnum
                .SingleOrDefault(k => k.Value == alg);
            var encryption = Constants.MappingNameToJweEncEnum
                .SingleOrDefault(k => k.Value == enc);
            if (jsonWebKey == null ||
                algo.IsDefault() || 
                encryption.IsDefault())
            {
                return entry;
            }

            // Construct the JWE protected header
            var jweProtectedHeader = new JweProtectedHeader
            {
                Alg = algo.Key,
                Enc = encryption.Key,
                Kid = jsonWebKey.Kid
            };

            var algorithm = _jweHelper.GetEncryptor(enc);
            var encryptionResult = algorithm.Encrypt(entry, 
                alg, 
                jweProtectedHeader, 
                jsonWebKey);

            var base64EncodedjweProtectedHeaderSerialized = jweProtectedHeader.SerializeWithDataContract().Base64Encode();
            var base64EncodedJweEncryptedKey = encryptionResult.EncryptedContentEncryptionKey.Base64EncodeBytes();
            var base64EncodedIv = encryptionResult.Iv.Base64EncodeBytes();
            var base64EncodedCipherText = encryptionResult.CipherText.Base64EncodeBytes();
            var base64EncodedAuthenticationTag = encryptionResult.AuthenticationTag.Base64EncodeBytes();

            return base64EncodedjweProtectedHeaderSerialized + "." +
                   base64EncodedJweEncryptedKey + "." +
                   base64EncodedIv + "." +
                   base64EncodedCipherText + "." +
                   base64EncodedAuthenticationTag;
        }
    }
}
