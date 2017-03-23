﻿///////////////////////////////////////////
// SharePointContextSaml.cs
//
// Authors and credits: 
//   Wictor Wilén
//   Steve Peschka
//
// Download:
//  https://github.com/wictorwilen/SharePointContextSaml
//
// License:
//   Copyright 2014 Wictor Wilén
//   
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//   
//       http://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
//
///////////////////////////////////////////


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Web;
using System.Web.Configuration;
using Microsoft.IdentityModel.S2S.Tokens;
using Microsoft.SharePoint.Client;

// TODO: Replace this namespace with your web project default namespace

namespace WsFederationPoC
{
    /// <summary>
    ///     TokenHelper.cs extensions
    /// </summary>
    /// <remarks>
    ///     You need to modify the default TokenHelper.cs class declaration and add the partial keyword
    /// </remarks>
    public static partial class TokenHelper
    {
        /// <summary>
        ///     Claim provider types
        /// </summary>
        public enum ClaimProviderType
        {
            SAML,
            FBA //NOTE: Not tested at all as of now
        }

        /// <summary>
        ///     Identity Claim Type options
        /// </summary>
        /// <remarks>
        ///     Configured in web.config appSettings using the setting <c>spsaml:IdentityClaimType</c>
        /// </remarks>
        /// <example>
        ///     <code>
        /// &lt;add key="spsaml:IdentityClaimType" value="SMTP"/&gt;
        /// </code>
        /// </example>
        public enum IdentityClaimType
        {
            /// <summary>
            ///     Use e-mail address as identity claim
            /// </summary>
            SMTP,

            /// <summary>
            ///     Use UPN as identity claim
            /// </summary>
            UPN,

            /// <summary>
            ///     Use SIP address as identity claim
            /// </summary>
            SIP
        }


        private const string CLAIM_TYPE_EMAIL = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
        private const string CLAIM_TYPE_UPN = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
        private const string CLAIM_TYPE_SIP = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sip";
        private const string CLAIM_TYPE_SID = "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid";

        private const string CLAIMS_ID_TYPE_EMAIL = "smtp";
        private const string CLAIMS_ID_TYPE_UPN = "upn";
        private const string CLAIMS_ID_TYPE_SIP = "sip";
        private const string CLAIMS_ID_TYPE_SID = "sid";
        private const string CLAIMS_ID_TYPE_PRIMARYSID = "primarysid";

        private const int TokenLifetimeMinutes = 1000000;

        private static readonly string TrustedProviderName =
            WebConfigurationManager.AppSettings.Get("spsaml:TrustedProviderName");

        private static readonly string MembershipProviderName =
            WebConfigurationManager.AppSettings.Get("spsaml:MembershipProviderName");

        public static readonly IdentityClaimType DefaultIdentityClaimType =
            (IdentityClaimType)
            Enum.Parse(typeof(IdentityClaimType), WebConfigurationManager.AppSettings.Get("spsaml:IdentityClaimType"));

        public static readonly ClaimProviderType DefaultClaimProviderType =
            (ClaimProviderType)
            Enum.Parse(typeof(ClaimProviderType), WebConfigurationManager.AppSettings.Get("spsaml:ClaimProviderType"));


        /// <summary>
        ///     Retrieves an S2S client context with an access token signed by the application's private certificate on
        ///     behalf of the specified Claims Identity and intended for application at the targetApplicationUri using the
        ///     targetRealm. If no Realm is specified in web.config, an auth challenge will be issued to the
        ///     targetApplicationUri to discover it.
        /// </summary>
        /// <param name="targetApplicationUri">Url of the target SharePoint site</param>
        /// <param name="identity">Identity of the user on whose behalf to create the access token; use HttpContext.Current.User</param>
        /// <param name="UserIdentityClaimType">The claim type that is used as the identity claim for the user</param>
        /// <param name="IdentityClaimProviderType">The type of identity provider being used</param>
        /// <param name="UseAppOnlyClaim">Use an App Only claim</param>
        /// <returns>A ClientContext using an access token with an audience of the target application</returns>
        public static ClientContext GetS2SClientContextWithClaimsIdentity(
            Uri targetApplicationUri,
            ClaimsIdentity identity,
            IdentityClaimType UserIdentityClaimType,
            ClaimProviderType IdentityClaimProviderType,
            bool UseAppOnlyClaim)
        {
            var realm = string.IsNullOrEmpty(Realm) ? GetRealmFromTargetUrl(targetApplicationUri) : Realm;


            var accessToken = GetS2SClaimsAccessTokenWithClaims(
                targetApplicationUri,
                identity,
                UserIdentityClaimType,
                IdentityClaimProviderType,
                UseAppOnlyClaim);

            return GetClientContextWithAccessToken(targetApplicationUri.ToString(), accessToken);
        }


        /// <summary>
        ///     Retrieves an S2S access token signed by the application's private certificate on
        ///     behalf of the specified Claims Identity and intended for application at the targetApplicationUri using the
        ///     targetRealm. If no Realm is specified in web.config, an auth challenge will be issued to the
        ///     targetApplicationUri to discover it.
        /// </summary>
        /// <param name="targetApplicationUri">Url of the target SharePoint site</param>
        /// <param name="identity">Identity of the user on whose behalf to create the access token; use HttpContext.Current.User</param>
        /// <param name="UserIdentityClaimType">The claim type that is used as the identity claim for the user</param>
        /// <param name="IdentityClaimProviderType">The type of identity provider being used</param>
        /// <param name="UseAppOnlyClaim">Use an App Only claim</param>
        /// <returns></returns>
        public static string GetS2SClaimsAccessTokenWithClaims(
            Uri targetApplicationUri,
            ClaimsIdentity identity,
            IdentityClaimType UserIdentityClaimType,
            ClaimProviderType IdentityClaimProviderType,
            bool UseAppOnlyClaim)
        {
            //get the identity claim info first
            ClaimsUserIdClaim id = null;

            if (IdentityClaimProviderType == ClaimProviderType.SAML)
                id = RetrieveIdentityForSamlClaimsUser(identity, UserIdentityClaimType);
            else
                id = RetrieveIdentityForFbaClaimsUser(identity, UserIdentityClaimType);

            var realm = string.IsNullOrEmpty(Realm) ? GetRealmFromTargetUrl(targetApplicationUri) : Realm;

            var claims = identity != null
                ? GetClaimsWithClaimsIdentity(identity, UserIdentityClaimType, id, IdentityClaimProviderType)
                : null;

            return IssueToken(
                ClientId,
                IssuerId,
                realm,
                SharePointPrincipal,
                realm,
                targetApplicationUri.Authority,
                true,
                claims,
                UseAppOnlyClaim,
                id.ClaimsIdClaimType != CLAIMS_ID_TYPE_UPN,
                id.ClaimsIdClaimType,
                id.ClaimsIdClaimValue);
        }

        private static string IssueToken(
            string sourceApplication,
            string issuerApplication,
            string sourceRealm,
            string targetApplication,
            string targetRealm,
            string targetApplicationHostName,
            bool trustedForDelegation,
            IEnumerable<JsonWebTokenClaim> claims,
            bool appOnly = false,
            bool addSamlClaim = false,
            string samlClaimType = "",
            string samlClaimValue = "")
        {
            if (null == SigningCredentials)
                throw new InvalidOperationException("SigningCredentials was not initialized");

            #region Actor token

            var issuer = string.IsNullOrEmpty(sourceRealm)
                ? issuerApplication
                : string.Format("{0}@{1}", issuerApplication, sourceRealm);
            var nameid = string.IsNullOrEmpty(sourceRealm)
                ? sourceApplication
                : string.Format("{0}@{1}", sourceApplication, sourceRealm);
            var audience = string.Format("{0}/{1}@{2}", targetApplication, targetApplicationHostName, targetRealm);

            var actorClaims = new List<JsonWebTokenClaim>();
            actorClaims.Add(new JsonWebTokenClaim(JsonWebTokenConstants.ReservedClaims.NameIdentifier, nameid));
            if (trustedForDelegation && !appOnly)
                actorClaims.Add(new JsonWebTokenClaim(TrustedForImpersonationClaimType, "true"));


            if (addSamlClaim)
                actorClaims.Add(new JsonWebTokenClaim(samlClaimType, samlClaimValue));

            // Create token
            var actorToken = new JsonWebSecurityToken(
                issuer,
                audience,
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(TokenLifetimeMinutes),
                signingCredentials: SigningCredentials,
                claims: actorClaims);

            var actorTokenString = new JsonWebSecurityTokenHandler().WriteTokenAsString(actorToken);

            if (appOnly)
                return actorTokenString;

            #endregion Actor token

            #region Outer token

            var outerClaims = null == claims ? new List<JsonWebTokenClaim>() : new List<JsonWebTokenClaim>(claims);
            outerClaims.Add(new JsonWebTokenClaim(ActorTokenClaimType, actorTokenString));

            if (addSamlClaim)
                outerClaims.Add(new JsonWebTokenClaim(samlClaimType, samlClaimValue));

            var jsonToken = new JsonWebSecurityToken(
                nameid, // outer token issuer should match actor token nameid
                audience,
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(10),
                outerClaims);

            var accessToken = new JsonWebSecurityTokenHandler().WriteTokenAsString(jsonToken);

            #endregion Outer token

            return accessToken;
        }

        private static string GetClaimFromClaimsId(ClaimsIdentity identity, string type)
        {
            if (identity.IsAuthenticated)
                foreach (var claim in identity.Claims)
                    if (claim.Type == type)
                        return claim.Value;
            return null;
        }

        /// <summary>
        ///     Retrieves the identity for the ClaimsIdentity
        /// </summary>
        /// <param name="identity">The Claims Identity</param>
        /// <param name="SamlIdentityClaimType">The Claims Identity Type</param>
        /// <returns></returns>
        private static ClaimsUserIdClaim RetrieveIdentityForSamlClaimsUser(ClaimsIdentity identity,
            IdentityClaimType SamlIdentityClaimType)
        {
            var id = new ClaimsUserIdClaim();
            try
            {
                if (identity.IsAuthenticated)
                {
                    //get the claim type we're looking for
                    var claimType = CLAIM_TYPE_EMAIL;
                    id.ClaimsIdClaimType = CLAIMS_ID_TYPE_EMAIL;

                    //since the vast majority of the time the id claim is email, we'll start out with that
                    //as our default position and only check if that isn't the case
                    if (SamlIdentityClaimType != IdentityClaimType.SMTP)
                        switch (SamlIdentityClaimType)
                        {
                            case IdentityClaimType.UPN:
                                claimType = CLAIM_TYPE_UPN;
                                id.ClaimsIdClaimType = CLAIMS_ID_TYPE_UPN;
                                break;
                            default:
                                claimType = CLAIM_TYPE_SIP;
                                id.ClaimsIdClaimType = CLAIMS_ID_TYPE_SIP;
                                break;
                        }

                    foreach (var claim in identity.Claims)
                        if (claim.Type == claimType)
                        {
                            id.ClaimsIdClaimValue = claim.Value;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                //not going to do anything here; could look for a missing identity claim but instead will just
                //return an empty string
                Debug.WriteLine(ex.Message);
            }

            return id;
        }


        /// <summary>
        ///     NOT IMPLEMENTED
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="SamlIdentityClaimType"></param>
        /// <returns></returns>
        private static ClaimsUserIdClaim RetrieveIdentityForFbaClaimsUser(ClaimsIdentity identity,
            IdentityClaimType SamlIdentityClaimType)
        {
            throw new NotImplementedException();
        }

        private static JsonWebTokenClaim[] GetClaimsWithClaimsIdentity(ClaimsIdentity indentity,
            IdentityClaimType SamlIdentityClaimType, ClaimsUserIdClaim id, ClaimProviderType IdentityClaimProviderType)
        {
            //if an identity claim was not found, then exit
            if (string.IsNullOrEmpty(id.ClaimsIdClaimValue))
                return null;

            var claimSet = new Hashtable();

            //you always need nii claim, so add that
            claimSet.Add("nii", "temp");

            //set up the nii claim and then add the smtp or sip claim separately
            if (IdentityClaimProviderType == ClaimProviderType.SAML)
                claimSet["nii"] = "trusted:" + TrustedProviderName.ToLower();
                    //was urn:office:idp:trusted:, but this does not seem to align with what SPIdentityClaimMapper uses
            else
                claimSet["nii"] = "urn:office:idp:forms:" + MembershipProviderName.ToLower();

            //plug in UPN claim if we're using that
            if (id.ClaimsIdClaimType == CLAIMS_ID_TYPE_UPN)
                claimSet.Add("upn", id.ClaimsIdClaimValue.ToLower());

            if (GetClaimFromClaimsId(indentity, CLAIM_TYPE_SID) != null)
            {
                claimSet[CLAIMS_ID_TYPE_SID] = GetClaimFromClaimsId(indentity, CLAIM_TYPE_SID);
                claimSet[CLAIMS_ID_TYPE_PRIMARYSID] = GetClaimFromClaimsId(indentity, CLAIM_TYPE_SID);
            }

            //now create the JsonWebTokenClaim array
            var claimList = new List<JsonWebTokenClaim>();

            foreach (string key in claimSet.Keys)
                claimList.Add(new JsonWebTokenClaim(key, (string) claimSet[key]));

            return claimList.ToArray();
        }

        //simple class used to hold instance variables for ID claim values
        private class ClaimsUserIdClaim
        {
            public string ClaimsIdClaimType { get; set; }
            public string ClaimsIdClaimValue { get; set; }
        }
    }

    #region HighTrust with SAML

    /// <summary>
    ///     Encapsulates all the information from SharePoint in HighTrust mode with SAML Claims.
    /// </summary>
    public class SharePointHighTrustSamlContext : SharePointContext
    {
        public SharePointHighTrustSamlContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage, string spClientTag,
            string spProductNumber, ClaimsIdentity logonUserIdentity)
            : base(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber)
        {
            if (logonUserIdentity == null)
                throw new ArgumentNullException("logonUserIdentity");

            LogonUserIdentity = logonUserIdentity;
        }

        /// <summary>
        ///     The Claims identity for the current user.
        /// </summary>
        public ClaimsIdentity LogonUserIdentity { get; }

        public override string UserAccessTokenForSPHost
        {
            get
            {
                if (SPHostUrl == null)
                    return null;

                return GetAccessTokenString(ref userAccessTokenForSPHost,
                    () => TokenHelper.GetS2SClaimsAccessTokenWithClaims(
                        SPHostUrl,
                        LogonUserIdentity,
                        TokenHelper.DefaultIdentityClaimType,
                        TokenHelper.DefaultClaimProviderType,
                        false
                    ));
            }
        }

        public override string UserAccessTokenForSPAppWeb
        {
            get
            {
                if (SPAppWebUrl == null)
                    return null;

                return GetAccessTokenString(ref userAccessTokenForSPAppWeb,
                    () => TokenHelper.GetS2SClaimsAccessTokenWithClaims(
                        SPAppWebUrl,
                        LogonUserIdentity,
                        TokenHelper.DefaultIdentityClaimType,
                        TokenHelper.DefaultClaimProviderType,
                        false
                    ));
            }
        }

        public override string AppOnlyAccessTokenForSPHost
        {
            get
            {
                return GetAccessTokenString(ref appOnlyAccessTokenForSPHost,
                    () => TokenHelper.GetS2SClaimsAccessTokenWithClaims(
                        SPHostUrl,
                        null,
                        TokenHelper.DefaultIdentityClaimType,
                        TokenHelper.DefaultClaimProviderType,
                        false));
            }
        }

        public override string AppOnlyAccessTokenForSPAppWeb
        {
            get
            {
                if (SPAppWebUrl == null)
                    return null;

                return GetAccessTokenString(ref appOnlyAccessTokenForSPAppWeb,
                    () => TokenHelper.GetS2SClaimsAccessTokenWithClaims(
                        SPAppWebUrl,
                        null,
                        TokenHelper.DefaultIdentityClaimType,
                        TokenHelper.DefaultClaimProviderType,
                        false));
            }
        }

        /// <summary>
        ///     Ensures the access token is valid and returns it.
        /// </summary>
        /// <param name="accessToken">The access token to verify.</param>
        /// <param name="tokenRenewalHandler">The token renewal handler.</param>
        /// <returns>The access token string.</returns>
        private static string GetAccessTokenString(ref Tuple<string, DateTime> accessToken,
            Func<string> tokenRenewalHandler)
        {
            RenewAccessTokenIfNeeded(ref accessToken, tokenRenewalHandler);

            return IsAccessTokenValid(accessToken) ? accessToken.Item1 : null;
        }

        /// <summary>
        ///     Renews the access token if it is not valid.
        /// </summary>
        /// <param name="accessToken">The access token to renew.</param>
        /// <param name="tokenRenewalHandler">The token renewal handler.</param>
        private static void RenewAccessTokenIfNeeded(ref Tuple<string, DateTime> accessToken,
            Func<string> tokenRenewalHandler)
        {
            if (IsAccessTokenValid(accessToken))
                return;

            var expiresOn = DateTime.UtcNow.Add(TokenHelper.HighTrustAccessTokenLifetime);

            if (TokenHelper.HighTrustAccessTokenLifetime > AccessTokenLifetimeTolerance)
                expiresOn -= AccessTokenLifetimeTolerance;

            accessToken = Tuple.Create(tokenRenewalHandler(), expiresOn);
        }

        /// <summary>
        ///     Creates app only ClientContext for the SharePoint host.
        /// </summary>
        /// <returns>A ClientContext instance.</returns>
        public override ClientContext CreateAppOnlyClientContextForSPHost()
        {
            return TokenHelper.GetS2SClientContextWithClaimsIdentity(SPHostUrl,
                LogonUserIdentity, TokenHelper.DefaultIdentityClaimType,
                TokenHelper.DefaultClaimProviderType, true);
        }

        /// <summary>
        ///     Creates an app only ClientContext for the SharePoint app web.
        /// </summary>
        /// <returns>A ClientContext instance.</returns>
        public override ClientContext CreateAppOnlyClientContextForSPAppWeb()
        {
            return TokenHelper.GetS2SClientContextWithClaimsIdentity(SPAppWebUrl,
                LogonUserIdentity, TokenHelper.DefaultIdentityClaimType,
                TokenHelper.DefaultClaimProviderType, true);
        }
    }

    /// <summary>
    ///     Default provider for SharePointHighTrustSamlContext.
    /// </summary>
    public class SharePointHighTrustSamlContextProvider : SharePointHighTrustContextProvider
    {
        private const string SPContextKey = "SPContext";

        protected override SharePointContext CreateSharePointContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage,
            string spClientTag, string spProductNumber, HttpRequestBase httpRequest)
        {
            var logonUserIdentity = HttpContext.Current.User.Identity as ClaimsIdentity;
            if (logonUserIdentity == null || !logonUserIdentity.IsAuthenticated)
                return null;

            return new SharePointHighTrustSamlContext(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber,
                logonUserIdentity);
        }

        protected override bool ValidateSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
        {
            var spHighTrustContext = spContext as SharePointHighTrustSamlContext;

            if (spHighTrustContext != null)
            {
                var spHostUrl = SharePointContext.GetSPHostUrl(httpContext.Request);
                ClaimsIdentity logonUserIdentity = httpContext.Request.LogonUserIdentity;

                return spHostUrl == spHighTrustContext.SPHostUrl &&
                       logonUserIdentity != null &&
                       logonUserIdentity.IsAuthenticated;
            }

            return false;
        }

        protected override SharePointContext LoadSharePointContext(HttpContextBase httpContext)
        {
            return httpContext.Session[SPContextKey] as SharePointHighTrustSamlContext;
        }

        protected override void SaveSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
        {
            httpContext.Session[SPContextKey] = spContext as SharePointHighTrustSamlContext;
        }

        public override SharePointContext GetSharePointContext(HttpContextBase httpContext)
        {
            return CreateSharePointContext(httpContext.Request);
        }

        public override SharePointContext CreateSharePointContext(HttpRequestBase httpRequest)
        {
            if (httpRequest == null)
                throw new ArgumentNullException("httpRequest");

            // SPHostUrl
            var spHostUrl = GetUrlParameterFromRequest(httpRequest, SharePointContext.SPHostUrlKey);
            if (spHostUrl == null)
                return null;

            // SPAppWebUrl
            var spAppWebUrl = GetUrlParameterFromRequest(httpRequest, SharePointContext.SPAppWebUrlKey);

            // SPLanguage
            var spLanguage = GetStringParameterFromRequest(httpRequest, SharePointContext.SPLanguageKey);
            if (string.IsNullOrEmpty(spLanguage))
                return null;

            // SPClientTag
            var spClientTag = GetStringParameterFromRequest(httpRequest, SharePointContext.SPClientTagKey);
            if (string.IsNullOrEmpty(spClientTag))
                return null;

            // SPProductNumber
            var spProductNumber = GetStringParameterFromRequest(httpRequest, SharePointContext.SPProductNumberKey);
            if (string.IsNullOrEmpty(spProductNumber))
                return null;

            return CreateSharePointContext(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber, httpRequest);
        }

        private string GetStringParameterFromRequest(HttpRequestBase httpRequest, string parameterName)
        {
            var str = httpRequest.QueryString[parameterName];
            if (string.IsNullOrEmpty(str))
                str = httpRequest.Headers[parameterName];

            return str;
        }

        private Uri GetUrlParameterFromRequest(HttpRequestBase httpRequest, string parameterName)
        {
            var hostUrlString = TokenHelper.EnsureTrailingSlash(httpRequest.QueryString[parameterName]);
            if (string.IsNullOrEmpty(hostUrlString))
                hostUrlString = TokenHelper.EnsureTrailingSlash(httpRequest.Headers[parameterName]);

            Uri url;
            if (Uri.TryCreate(HttpUtility.UrlDecode(hostUrlString), UriKind.Absolute, out url) &&
                (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps))
                return url;

            return null;
        }
    }

    #endregion HighTrust
}