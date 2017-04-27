using NUnit.Framework;

namespace WsFederationPoC.Tests
{
    [TestFixture]
    public class ClaimsEncodingTest
    {

        [Test]
        public void TestClaimsEncodingWindowsUserNoClaimsEncoding()
        {
            var result = ClaimsEncoding.Parse("contoso\\chris");
            Assert.That(result.IdendityClaim, Is.Null);
            Assert.That(result.ClaimType, Is.Null);
            Assert.That(result.ClaimValueType, Is.Null);
            Assert.That(result.AuthMode, Is.Null);
            Assert.That(result.OriginalIssuer, Is.Null);
            Assert.That(result.ClaimValue, Is.Null);
        }

        [Test]
        public void TestClaimsEncodingWindowsUser()
        {
            var result = ClaimsEncoding.Parse("i:0#.w|contoso\\chris");
            Assert.That(result.IdendityClaim, Is.EqualTo("i"));
            Assert.That(result.ClaimType, Is.EqualTo("#"));
            Assert.That(result.ClaimValueType, Is.EqualTo("."));
            Assert.That(result.AuthMode, Is.EqualTo("w"));
            Assert.That(result.OriginalIssuer, Is.Null);
            Assert.That(result.ClaimValue, Is.EqualTo("contoso\\chris"));
        }

        [Test]
        public void TestClaimsEncodingWindowsAuthenticatedUsersgroup()
        {
            var result = ClaimsEncoding.Parse("c:0!.s|windows");
            Assert.That(result.IdendityClaim, Is.EqualTo("c"));
            Assert.That(result.ClaimType, Is.EqualTo("!"));
            Assert.That(result.ClaimValueType, Is.EqualTo("."));
            Assert.That(result.AuthMode, Is.EqualTo("s"));
            Assert.That(result.OriginalIssuer, Is.Null);
            Assert.That(result.ClaimValue, Is.EqualTo("windows"));
        }

        [Test]
        public void TestClaimsEncodingActiveDirectoryUsersgroupwithSid()
        {
            var result = ClaimsEncoding.Parse("c:0+.w|s-1-2-34-1234567890-1234567890-1234567890-1234");
            Assert.That(result.IdendityClaim, Is.EqualTo("c"));
            Assert.That(result.ClaimType, Is.EqualTo("+"));
            Assert.That(result.ClaimValueType, Is.EqualTo("."));
            Assert.That(result.AuthMode, Is.EqualTo("w"));
            Assert.That(result.OriginalIssuer, Is.Null);
            Assert.That(result.ClaimValue, Is.EqualTo("s-1-2-34-1234567890-1234567890-1234567890-1234"));
        }

        [Test]
        public void TestClaimsEncodingCustomAdfsClaimprovider()
        {
            var result = ClaimsEncoding.Parse("i:0ǵ.t|custom-adfs|First.Last");
            Assert.That(result.IdendityClaim, Is.EqualTo("i"));
            Assert.That(result.ClaimType, Is.EqualTo("ǵ"));
            Assert.That(result.ClaimValueType, Is.EqualTo("."));
            Assert.That(result.AuthMode, Is.EqualTo("t"));
            Assert.That(result.OriginalIssuer, Is.EqualTo("custom-adfs"));
            Assert.That(result.ClaimValue, Is.EqualTo("First.Last"));
        }

        [Test]
        public void TestClaimsEncodingSamlAuthenticationTrustedUser()
        {
            var result = ClaimsEncoding.Parse("i:05.t|adfs|chris@contoso.com");
            Assert.That(result.IdendityClaim, Is.EqualTo("i"));
            Assert.That(result.ClaimType, Is.EqualTo("5"));
            Assert.That(result.ClaimValueType, Is.EqualTo("."));
            Assert.That(result.AuthMode, Is.EqualTo("t"));
            Assert.That(result.OriginalIssuer, Is.EqualTo("adfs"));
            Assert.That(result.ClaimValue, Is.EqualTo("chris@contoso.com"));
        }
        [Test]
        public void TestClaimsEncodingFormsBasedAuthentication()
        {
            var result = ClaimsEncoding.Parse("i:0#.f|mymembershipprovider|chris");
            Assert.That(result.IdendityClaim, Is.EqualTo("i"));
            Assert.That(result.ClaimType, Is.EqualTo("#"));
            Assert.That(result.ClaimValueType, Is.EqualTo("."));
            Assert.That(result.AuthMode, Is.EqualTo("f"));
            Assert.That(result.OriginalIssuer, Is.EqualTo("mymembershipprovider"));
            Assert.That(result.ClaimValue, Is.EqualTo("chris"));
        }
    }
}