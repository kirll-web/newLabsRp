using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using NUnit.Framework;

namespace TestProject2;

public class Tests
{
    private IWebDriver? _driver;
    private readonly string _textFieldId = "field";
    private readonly string _submitButtonId = "submit";

    private readonly string _rankCalculatedId = "RankCalculated";
    private readonly string _similarityCalculatedId = "SimilarityCalculated";
    private readonly string _similirityText = "4a5a";
    private readonly string _similirityLabel = "Плагиат:";
    
    [SetUp]
    public void Setup()
    {
        _driver = new ChromeDriver();
        _driver.Navigate().GoToUrl("http://localhost:5001/");
        _driver.Manage().Window.Maximize();
    }

    [Test]
    public void TestRankTextAndSimilarityForFirstInputText()
    {
        Thread.Sleep(2000);
        var inputField = _driver.FindElement(By.Id(_textFieldId));
        var submitButton = _driver.FindElement(By.Id(_submitButtonId));
   
        var text = _similirityText;
        var expectedRank = "0.5";
        var expectedSimilirity = $"{_similirityLabel} 0";
        
        inputField.Clear();
        inputField.SendKeys(text);
        submitButton.Click();
        Thread.Sleep(5000);
        
        var rankElement = _driver.FindElement(By.Id(_rankCalculatedId));
        var similarityElement = _driver.FindElement(By.Id(_similarityCalculatedId));
        string rankValue = rankElement.Text; 
        string similarityValue = similarityElement.Text;

        Assert.That(expectedRank, Is.EqualTo(rankValue));
        Assert.That(expectedSimilirity, Is.EqualTo(similarityValue));
    }
    
    [Test]
    public void TestSimilarityForSecondInputText()
    {
        Thread.Sleep(2000);
        var inputField = _driver.FindElement(By.Id(_textFieldId));
        var submitButton = _driver.FindElement(By.Id(_submitButtonId));
   
        var text = _similirityText;
        var expectedSimilirity = $"{_similirityLabel} 1";
        
        inputField.Clear();
        inputField.SendKeys(text);
        submitButton.Click();
        var similarityElement = _driver.FindElement(By.Id(_similarityCalculatedId));
        string similarityValue = similarityElement.Text;

        Assert.That(expectedSimilirity, Is.EqualTo(similarityValue));
    }
    
    [TearDown]
    public void TearDown()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
}
