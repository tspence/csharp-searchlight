using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Searchlight.Tests
{
    [TestClass]
    public class FlagTests
    {
        [SearchlightModel]
        [SearchlightFlag(Name = "TestFlag", Aliases = new string[] { "AliasOne", "AliasTwo" })]
        [SearchlightFlag(Name = "TestTwo")]
        public class FlagTestClass
        {
        }

        [TestMethod]
        public void TestFlagEnable()
        {
            var engine = new SearchlightEngine()
               .AddClass(typeof(FlagTestClass));

            // Test whether the flag can be found by its name
            var syntax = engine.Parse(new FetchRequest()
            {
                include = "TestFlag",
                table = "FlagTestClass"
            });
            Assert.AreEqual(1, syntax.Flags.Count);
            Assert.AreEqual("TestFlag", syntax.Flags[0].Name);
            Assert.AreEqual(true, syntax.HasFlag("TestFlag"));
            Assert.AreEqual(false, syntax.HasFlag("TestTwo"));
            Assert.AreEqual(false, syntax.HasFlag("SomeFlagThatDoesntExist"));

            // Test whether the flag can be found by its alias
            syntax = engine.Parse(new FetchRequest()
            {
                include = "AliasOne",
                table = "FlagTestClass"
            });
            Assert.AreEqual(1, syntax.Flags.Count);
            Assert.AreEqual("TestFlag", syntax.Flags[0].Name);
            Assert.AreEqual(true, syntax.HasFlag("TestFlag"));
            syntax = engine.Parse(new FetchRequest()
            {
                include = "AliasTwo",
                table = "FlagTestClass"
            });
            Assert.AreEqual(1, syntax.Flags.Count);
            Assert.AreEqual("TestFlag", syntax.Flags[0].Name);
            Assert.AreEqual(true, syntax.HasFlag("TestFlag"));
            Assert.AreEqual(false, syntax.HasFlag("TestTwo"));

            // Test whether the flag is false by default
            syntax = engine.Parse(new FetchRequest()
            {
                table = "FlagTestClass"
            });
            Assert.AreEqual(0, syntax.Flags.Count);
            Assert.AreEqual(false, syntax.HasFlag("TestFlag"));
            Assert.AreEqual(false, syntax.HasFlag("TestTwo"));

            // Test whether an unknown flag throws the correct error
            var ex = Assert.ThrowsException<IncludeNotFound>(() =>
            {
                engine.Parse(new FetchRequest()
                {
                    table = "FlagTestClass",
                    include = "TestFlag, AnUnknownAlias, AnotherUnknownAliasButThisOneDoesntGetTestedBecauseTheFirstOneFails",
                });
            });
            Assert.AreEqual("AnUnknownAlias", ex.IncludeName);
            Assert.AreEqual("TestFlag, AnUnknownAlias, AnotherUnknownAliasButThisOneDoesntGetTestedBecauseTheFirstOneFails", ex.OriginalInclude);
            Assert.IsTrue(ex.KnownIncludes.Contains("TestFlag"));
            
            // Check whether two flags can be specified
            syntax = engine.Parse(new FetchRequest()
            {
                table = "FlagTestClass",
                include = "TestFlag, TestTwo"
            });
            Assert.AreEqual(2, syntax.Flags.Count);
            Assert.AreEqual(true, syntax.HasFlag("TestFlag"));
            Assert.AreEqual(true, syntax.HasFlag("TestTwo"));
        }
    }
}
