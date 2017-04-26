$emailClaimMap = New-SPClaimTypeMapping 
	-IncomingClaimType "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress" 
	-IncomingClaimTypeDisplayName "EmailAddress" 
	-SameAsIncoming
$upnClaimMap = New-SPClaimTypeMapping 
	-IncomingClaimType "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn" 
	-IncomingClaimTypeDisplayName "UPN"
	-SameAsIncoming
$roleClaimMap = New-SPClaimTypeMapping 
	-IncomingClaimType "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" 
	-IncomingClaimTypeDisplayName "Role" 
	-SameAsIncoming
$sidClaimMap = New-SPClaimTypeMapping 
	-IncomingClaimType "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid" 
	-IncomingClaimTypeDisplayName "SID" 
	-SameAsIncoming

$realm = "urn:sharepoint:swd"

$signInURL = “https://sts.acme.lab/adfs/ls"

$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2("<FullPathOfTheTokenSignCertFile>")
New-SPTrustedRootAuthority -Name "ACME Token Signing Cert" -Certificate $cert

New-SPTrustedIdentityTokenIssuer 
	-Name "ACME" -Description "ACME" 
	-realm $realm -ImportTrustCertificate $cert 
	-ClaimsMappings $emailClaimMap,$upnClaimMap,$roleClaimMap,$sidClaimMap 
	-SignInUrl $signInURL 
	-IdentifierClaim $emailClaimmap.InputClaimType

$trust = Get-SPTrustedIdentityTokenIssuer "ACME"
$trust.ClaimProviderName = "LDAPCP"
$trust.ClaimTypes.Add("http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsaccountname")
$trust.ClaimTypes.Add("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")
$trust.Update()

$map1 = new-SPClaimTypeMapping 
    -IncomingClaimType "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsaccountname" 
    -IncomingClaimTypeDisplayName "windowsaccountname" 
    -SameAsIncoming

Add-SPClaimTypeMapping -Identity $map1 -TrustedIdentityTokenIssuer $trust

$map2 = New-SPClaimTypeMapping 
    -IncomingClaimType "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname" 
    -IncomingClaimTypeDisplayName "givenname" 
    -SameAsIncoming

Add-SPClaimTypeMapping -Identity $map2 -TrustedIdentityTokenIssuer $trust