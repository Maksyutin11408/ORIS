using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace MiniTemplateEngine.Tests;

[TestClass]
public class TemplateRendererTests
{
    private readonly IHtmlTemplateRenderer _renderer = new HtmlTemplateRenderer();

    public class Client { public string? Name { get; set; } }

    [TestMethod]
    public void SimpleVariablesTest()
    {
        var model = new { Name = "John", Age = 20 };
        string template = "Имя: ${Name}, Возраст: ${Age}";

        string result = _renderer.RenderFromString(template, model);

        Assert.AreEqual("Имя: John, Возраст: 20", result);
    }

    [TestMethod]
    public void IfConditionTrueTest()
    {
        var model = new { IsTrue = true };
        string template = "$if (IsTrue)True$elseFalse$endif";

        string result = _renderer.RenderFromString(template, model);

        Assert.AreEqual("True", result);
    }
    [TestMethod]
    public void IfConditionFalseTest()
    {
        var model = new { IsTrue = false };
        string template = "$if (IsTrue)True$elseFalse$endif";

        string result = _renderer.RenderFromString(template, model);

        Assert.AreEqual("False", result);
    }

    [TestMethod]
    public void ForeachLoop()
    {
        var model = new { Items = new List<Client> { new() { Name = "Ann" }, new() { Name = "Josh" } } };
        string template = "$foreach (x in Items)[${Name}]$endfor";

        string result = _renderer.RenderFromString(template, model);

        Assert.AreEqual("[Ann][Josh]", result);
    }

    [TestMethod]
    public void IfInsideForeachTest()
    {
        var model = new
        {
            Users = new List<dynamic>
            {
                new { Name = "Jane", IsAdmin = true },
                new { Name = "Katherine", IsAdmin = false }
            }
        };
        string template = "$foreach (u in Users)${Name}: $if (IsAdmin)Админ$elseЮзер$endif; $endfor";

        string result = _renderer.RenderFromString(template, model);

        Assert.AreEqual("Jane: Админ; Katherine: Юзер; ", result);
    }
}
