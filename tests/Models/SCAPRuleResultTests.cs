using Xunit;
using openrmf_upload_api.Models;
using System;

namespace tests.Models
{
    public class SCAPRuleResultSetTests
    {
        [Fact]
        public void Test_NewSCAPRuleResultIsValid()
        {
            SCAPRuleResult sr = new SCAPRuleResult();
            Assert.True(sr != null);
        }
    
        [Fact]
        public void Test_NewSCAPRuleResultWithDataIsValid()
        {
            SCAPRuleResult sr = new SCAPRuleResult();
            sr.ruleId = "1234";
            sr.result = "pass";

            // test things out
            Assert.True(sr != null);
            Assert.True (!string.IsNullOrEmpty(sr.ruleId));
            Assert.True (!string.IsNullOrEmpty(sr.result));
        }
    }
}
