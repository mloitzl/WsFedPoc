using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Principal;
using System.Web.Configuration;
using System.Web.Security;
using Microsoft.IdentityModel.S2S.Tokens;
using Microsoft.SharePoint.Client;
//*************************************************
//SPSAML

//*************************************************

namespace WsFederationPoC
{
    /// <summary>
    ///     Summary description for ClaimsTokenHelper
    /// </summary>
    public partial class TokenHelper
    {
        public enum ClaimProviderType
        {
            SAML,
            FBA
        }

        public enum IdentityClaimType
        {
            SMTP,
            UPN,
            SIP
        }

        private const string CLAIM_TYPE_EMAIL = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
        private const string CLAIM_TYPE_UPN = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
        private const string CLAIM_TYPE_SIP = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sip";

        private const string CLAIMS_ID_TYPE_EMAIL = "smtp";
        private const string CLAIMS_ID_TYPE_UPN = "upn";
        private const string CLAIMS_ID_TYPE_SIP = "sip";

        //**********************************************************************************************************

        //*********************************************************************************************
        //SPSAML
        private static readonly string TrustedProviderName =
            WebConfigurationManager.AppSettings.Get("TrustedProviderName");

        private static readonly string MembershipProviderName =
            WebConfigurationManager.AppSettings.Get("MembershipProviderName");

        //**********************************************************************************************************
        //SPSAML

        /// <summary>
        ///     Retrieves an S2S client context with an access token signed by the application's private certificate on
        ///     behalf of the specified IPrincipal and intended for application at the targetApplicationUri using the
        ///     targetRealm. If no Realm is specified in web.config, an auth challenge will be issued to the
        ///     targetApplicationUri to discover it.
        /// </summary>
        /// <param name="targetApplicationUri">Url of the target SharePoint site</param>
        /// <param name="UserPrincipal">
        ///     Identity of the user on whose behalf to create the access token; use
        ///     HttpContext.Current.User
        /// </param>
        /// <param name="SamlIdentityClaimType">The claim type that is used as the identity claim for the user</param>
        /// <param name="IdentityClaimProviderType">The type of identity provider being used</param>
        /// <returns>A ClientContext using an access token with an audience of the target application</returns>
        public static ClientContext GetS2SClientContextWithClaimsIdentity(
            Uri targetApplicationUri,
            IPrincipal UserPrincipal,
            IdentityClaimType UserIdentityClaimType,
            ClaimProviderType IdentityClaimProviderType,
            bool UseAppOnlyClaim)
        {
            //get the identity claim info first
            ClaimsUserIdClaim id = null;

            if (IdentityClaimProviderType == ClaimProviderType.SAML)
                id = RetrieveIdentityForSamlClaimsUser(UserPrincipal, UserIdentityClaimType);
            else
                id = RetrieveIdentityForFbaClaimsUser(UserPrincipal, UserIdentityClaimType);

            var realm = string.IsNullOrEmpty(Realm) ? GetRealmFromTargetUrl(targetApplicationUri) : Realm;

            var claims = UserPrincipal != null
                ? GetClaimsWithClaimsIdentity(UserPrincipal, UserIdentityClaimType, id, IdentityClaimProviderType)
                : null;

            var accessToken = GetS2SClaimsAccessTokenWithClaims(targetApplicationUri.Authority, realm,
                claims, id.ClaimsIdClaimType, id.ClaimsIdClaimValue, UseAppOnlyClaim);

            return GetClientContextWithAccessToken(targetApplicationUri.ToString(), accessToken);
        }


        private static JsonWebTokenClaim[] GetClaimsWithClaimsIdentity(
            IPrincipal UserPrincipal,
            IdentityClaimType SamlIdentityClaimType, ClaimsUserIdClaim id,
            ClaimProviderType IdentityClaimProviderType)
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

            //now create the JsonWebTokenClaim array
            var claimList = new List<JsonWebTokenClaim>();

            foreach (string key in claimSet.Keys)
                claimList.Add(new JsonWebTokenClaim(key, (string) claimSet[key]));

            return claimList.ToArray();
        }

        private static ClaimsUserIdClaim RetrieveIdentityForSamlClaimsUser(
            IPrincipal UserPrincipal,
            IdentityClaimType SamlIdentityClaimType)
        {
            var id = new ClaimsUserIdClaim();

            try
            {
                if (UserPrincipal.Identity.IsAuthenticated)
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

                    //debug testing only 
#if DEBUG
                    if (SamlIdentityClaimType == IdentityClaimType.SIP)
                        id.ClaimsIdClaimValue = "darrins@vbtoys.com";
#endif

                    //save the claim type
                    var cp = UserPrincipal as ClaimsPrincipal;

                    if (cp != null)
                        foreach (var claim in cp.Claims)
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

        //this is an implementation based on using role claims for SMTP and SIP where the 
        //claim value starts with "SMTP:" or "SIP:".  There isn't a standard way to implement
        //this so you can choose whatever method you want and then update this method appropriately
        private static ClaimsUserIdClaim RetrieveIdentityForFbaClaimsUser(
            IPrincipal UserPrincipal,
            IdentityClaimType SamlIdentityClaimType)
        {
            var id = new ClaimsUserIdClaim();

            try
            {
                if (UserPrincipal.Identity.IsAuthenticated)
                {
                    //get the claim type we're looking for
                    id.ClaimsIdClaimType = CLAIMS_ID_TYPE_EMAIL;

                    //since the vast majority of the time the id claim is email, we'll start out with that
                    //as our default position and only check if that isn't the case
                    if (SamlIdentityClaimType != IdentityClaimType.SMTP)
                        switch (SamlIdentityClaimType)
                        {
                            case IdentityClaimType.UPN:
                                id.ClaimsIdClaimType = CLAIMS_ID_TYPE_UPN;
                                break;
                            default:
                                id.ClaimsIdClaimType = CLAIMS_ID_TYPE_SIP;
                                break;
                        }

                    var roles = Roles.GetRolesForUser();

                    foreach (var role in roles)
                        if (role.ToLower().StartsWith(id.ClaimsIdClaimType.ToLower()))
                        {
                            id.ClaimsIdClaimValue = role.Substring(role.IndexOf(":") + 1);
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


        private static string GetS2SClaimsAccessTokenWithClaims(
            string targetApplicationHostName,
            string targetRealm,
            IEnumerable<JsonWebTokenClaim> claims,
            string IdClaimType,
            string IdClaimValue,
            bool UseAppOnlyClaim)
        {
            return IssueToken(
                ClientId,
                IssuerId,
                targetRealm,
                SharePointPrincipal,
                targetRealm,
                targetApplicationHostName,
                true,
                claims,
                UseAppOnlyClaim,
                IdClaimType != CLAIMS_ID_TYPE_UPN,
                IdClaimType,
                IdClaimValue);
        }

        //*********************************************************************************************


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

            //****************************************************************************
            //SPSAML

            //if (samlClaimType == SAML_ID_CLAIM_TYPE_UPN)
            //{
            //    addSamlClaim = true;
            //    samlClaimType = SAML_ID_CLAIM_TYPE_SIP;
            //    samlClaimValue = "bluto2@toys.com";
            //}

            if (addSamlClaim)
                actorClaims.Add(new JsonWebTokenClaim(samlClaimType, samlClaimValue));
            //actorClaims.Add(new JsonWebTokenClaim("smtp", "speschka@vbtoys.com"));
            //****************************************************************************

            // Create token
            var actorToken = new JsonWebSecurityToken(
                issuer,
                audience,
                DateTime.UtcNow,
                DateTime.UtcNow.Add(HighTrustAccessTokenLifetime),
                signingCredentials: SigningCredentials,
                claims: actorClaims);

            var actorTokenString = new JsonWebSecurityTokenHandler().WriteTokenAsString(actorToken);

            if (appOnly)
                return actorTokenString;

            #endregion Actor token

            #region Outer token

            var outerClaims = null == claims
                ? new List<JsonWebTokenClaim>()
                : new List<JsonWebTokenClaim>(claims);
            outerClaims.Add(new JsonWebTokenClaim(ActorTokenClaimType, actorTokenString));

            //****************************************************************************
            //SPSAML
            if (addSamlClaim)
                outerClaims.Add(new JsonWebTokenClaim(samlClaimType, samlClaimValue));
            //****************************************************************************

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

        //simple class used to hold instance variables for ID claim values
        private class ClaimsUserIdClaim
        {
            public string ClaimsIdClaimType { get; set; }
            public string ClaimsIdClaimValue { get; set; }
        }
    }
}