using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.EntityFramework
{
    public static class UserEx
    {
        public static User WithExternalId(this User user, string externalId)
            => user.WithExternalId(user.AuthenticationType, externalId);
        public static User WithExternalId(this User user,
            string authenticationType, string externalId)
        {
            if (string.IsNullOrEmpty(authenticationType))
                throw new ArgumentOutOfRangeException(nameof(authenticationType));
            if (string.IsNullOrEmpty(externalId))
                throw new ArgumentOutOfRangeException(nameof(externalId));

            var claimId = $"/externalId/{authenticationType}";
            return user with {
                Claims = user.Claims.SetItem(claimId, externalId),
            };
        }

        public static string? TryGetExternalId(this User user)
            => user.TryGetExternalId(user.AuthenticationType);
        public static string? TryGetExternalId(this User user, string authenticationType)
        {
            if (string.IsNullOrEmpty(authenticationType))
                throw new ArgumentOutOfRangeException(nameof(authenticationType));

            var claimId = $"/externalId/{authenticationType}";
            return user.Claims.GetValueOrDefault(claimId);
        }

        public static string GetExternalId(this User user)
            => user.GetExternalId(user.AuthenticationType);
        public static string GetExternalId(this User user, string authenticationType)
        {
            if (string.IsNullOrEmpty(authenticationType))
                throw new ArgumentOutOfRangeException(nameof(authenticationType));

            var claimId = $"/externalId/{authenticationType}";
            return user.Claims[claimId];
        }

        public static IEnumerable<(string AuthenticationType, string ExternalId)>
            ListExternalIds(this User user)
        {
            var prefix = "/externalId/";
            foreach (var claim in user.Claims)
                if (claim.Key.StartsWith(prefix))
                    yield return (claim.Key.Substring(prefix.Length), claim.Value);
        }
    }
}
