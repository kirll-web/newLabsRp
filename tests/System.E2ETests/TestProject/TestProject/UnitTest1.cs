using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace TestProject;
public class Tests
{
  
    [SetUp]
    public void SetUp()
    {
      
    }

    [Test]
    public void TestSuccessfulLogin()
    {
        Assert.Equals(true, true);
    }

    [TearDown]
    public void TearDown()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
}
