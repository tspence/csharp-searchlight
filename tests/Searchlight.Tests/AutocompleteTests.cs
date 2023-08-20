using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Searchlight.Tests;

[TestClass]
public class AutocompleteTests
{
    public SearchlightEngine GetTestEngine()
    {
        var source = new DataSource()
            .WithColumn("a", typeof(String))
            .WithColumn("b", typeof(Int32))
            .WithColumn("colLong", typeof(Int64))
            .WithColumn("colNullableGuid", typeof(Nullable<Guid>))
            .WithColumn("colULong", typeof(UInt64))
            .WithColumn("colNullableULong", typeof(Nullable<UInt64>))
            .WithColumn("colGuid", typeof(Guid));
        source.TableName = "source";

        var engine = new SearchlightEngine().AddDataSource(source);
        return engine;
    }

    [TestMethod]
    public void EmptyStringAutocomplete()
    {
        var engine = GetTestEngine();
        var completion = engine.AutocompleteFilter("source", "", 0);
        Assert.IsNotNull(completion);
        Assert.IsFalse(completion.isIncomplete);
        Assert.AreEqual(7, completion.items.Count);
        Assert.AreEqual("a", completion.items[0].label);
        Assert.AreEqual("b", completion.items[1].label);
        Assert.AreEqual("colGuid", completion.items[2].label);
        Assert.AreEqual("colLong", completion.items[3].label);
        Assert.AreEqual("colNullableGuid", completion.items[4].label);
        Assert.AreEqual("colNullableULong", completion.items[5].label);
        Assert.AreEqual("colULong", completion.items[6].label);
    }

    [TestMethod]
    public void BasicAutocomplete()
    {
        var engine = GetTestEngine();
        var completion = engine.AutocompleteFilter("source", "col eq 0", 3);
        Assert.IsNotNull(completion);
        Assert.IsFalse(completion.isIncomplete);
        Assert.AreEqual(5, completion.items.Count);
        Assert.AreEqual("colGuid", completion.items[0].label);
        Assert.AreEqual("colLong", completion.items[1].label);
        Assert.AreEqual("colNullableGuid", completion.items[2].label);
        Assert.AreEqual("colNullableULong", completion.items[3].label);
        Assert.AreEqual("colULong", completion.items[4].label);
        
        completion = engine.AutocompleteFilter("source", "colNullableGuid eq 0", 10);
        Assert.IsNotNull(completion);
        Assert.IsFalse(completion.isIncomplete);
        Assert.AreEqual(2, completion.items.Count);
        Assert.AreEqual("colNullableGuid", completion.items[0].label);
        Assert.AreEqual("colNullableULong", completion.items[1].label);
    }
    
    [TestMethod]
    public void ConjunctionAutocomplete()
    {
        var engine = GetTestEngine();
        var completion = engine.AutocompleteFilter("source", "colLong eq 0 asdasd", 14);
        Assert.IsNotNull(completion);
        Assert.IsFalse(completion.isIncomplete);
        Assert.AreEqual(1, completion.items.Count);
        Assert.AreEqual("AND", completion.items[0].label);
    }
    
    [TestMethod]
    public void OperatorAutocomplete()
    {
        var engine = GetTestEngine();
        var completion = engine.AutocompleteFilter("source", "colLong eq 0", 9);
        Assert.IsNotNull(completion);
        Assert.IsFalse(completion.isIncomplete);
        Assert.AreEqual(2, completion.items.Count);
        Assert.AreEqual("ENDSWITH", completion.items[0].label);
        Assert.AreEqual("EQ", completion.items[1].label);
    }
}