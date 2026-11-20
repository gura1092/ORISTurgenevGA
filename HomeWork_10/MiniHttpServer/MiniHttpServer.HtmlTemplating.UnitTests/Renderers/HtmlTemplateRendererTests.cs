using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.HtmlTemplating.UnitTests.Renderers;

[TestClass]
public class HtmlTemplateRendererTests
{
    private readonly HtmlTemplateRenderer _renderer = new();

    [TestMethod] public void RenderFromString_ValidProperty_ReplacesPlaceholder() { var model = new { Name = "Alex" }; var result = _renderer.RenderFromString("<h1>${Name}</h1>", model); Assert.AreEqual("<h1>Alex</h1>", result); }
    [TestMethod] public void RenderFromString_MultipleProperties_ReplacesAllPlaceholders() { var model = new { Name = "Alex", Age = 20 }; var result = _renderer.RenderFromString("${Name}:${Age}", model); Assert.AreEqual("Alex:20", result); }
    [TestMethod] public void RenderFromString_RepeatedProperty_ReplacesEveryOccurrence() { var result = _renderer.RenderFromString("${Name}/${Name}", new { Name = "A&B" }); Assert.AreEqual("A&amp;B/A&amp;B", result); }
    [TestMethod] public void RenderFromString_NestedProperty_ResolvesValue() { var result = _renderer.RenderFromString("${User.Name}", new { User = new { Name = "Maria" } }); Assert.AreEqual("Maria", result); }
    [TestMethod] public void RenderFromString_EmptyTemplate_ReturnsEmptyString() { Assert.AreEqual(string.Empty, _renderer.RenderFromString(string.Empty, new { Name = "Alex" })); }
    [TestMethod] public void RenderFromString_TrueCondition_RendersIfBlock() { var result = _renderer.RenderFromString("A$if(IsActive)B$else C$endif D", new { IsActive = true }); Assert.AreEqual("AB D", result); }
    [TestMethod] public void RenderFromString_FalseCondition_RendersElseBlock() { var result = _renderer.RenderFromString("A$if(IsActive)B$else C$endif D", new { IsActive = false }); Assert.AreEqual("A C D", result); }
    [TestMethod] public void RenderFromString_IfElse_RendersSelectedBranch() { var result = _renderer.RenderFromString("$if(Flag)yes$else no$endif", new { Flag = false }); Assert.AreEqual(" no", result); }
    [TestMethod] public void RenderFromString_Collection_RendersAllItems() { var result = _renderer.RenderFromString("$foreach(var item in Items)${item};$endfor", new { Items = new[] { "a", "b", "c" } }); Assert.AreEqual("a;b;c;", result); }
    [TestMethod] public void RenderFromString_EmptyCollection_RendersNothing() { var result = _renderer.RenderFromString("X$foreach(var item in Items)${item}$endforY", new { Items = Array.Empty<string>() }); Assert.AreEqual("XY", result); }
    [TestMethod] public void RenderFromString_NestedIf_RendersCorrectBranch() { var result = _renderer.RenderFromString("$if(A)$if(B)yes$else no$endif$else outer$endif", new { A = true, B = false }); Assert.AreEqual(" no", result); }
    [TestMethod] public void RenderFromString_NestedLoops_RendersEveryPair() { var result = _renderer.RenderFromString("$foreach(var x in X)$foreach(var y in Y)${x}${y};$endfor$endfor", new { X = new[] { "a", "b" }, Y = new[] { 1, 2 } }); Assert.AreEqual("a1;a2;b1;b2;", result); }
    [TestMethod] public void RenderFromString_IfInsideLoop_RendersConditionForEachItem() { var result = _renderer.RenderFromString("$foreach(var item in Items)$if(item.Active)${item.Name}$else -$endif;$endfor", new { Items = new[] { new { Name = "A", Active = true }, new { Name = "B", Active = false } } }); Assert.AreEqual("A; -;", result); }
    [TestMethod] public void RenderFromString_MissingProperty_ThrowsTemplateException() { Assert.ThrowsException<TemplateException>(() => _renderer.RenderFromString("${Missing}", new { Name = "A" })); }
    [TestMethod] public void RenderFromString_MissingEndIf_ThrowsTemplateException() { Assert.ThrowsException<TemplateException>(() => _renderer.RenderFromString("$if(Flag)yes", new { Flag = true })); }
    [TestMethod] public void RenderFromString_InvalidConditionType_ThrowsTemplateException() { Assert.ThrowsException<TemplateException>(() => _renderer.RenderFromString("$if(Name)yes$endif", new { Name = "Alex" })); }
    [TestMethod] public void RenderFromFile_ExistingFile_RendersContent() { string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid()+".html"); try { File.WriteAllText(file,"<p>${Name}</p>"); var result = _renderer.RenderFromFile(file,new { Name="Test" }); Assert.AreEqual("<p>Test</p>",result); } finally { if(File.Exists(file)) File.Delete(file); } }
}
