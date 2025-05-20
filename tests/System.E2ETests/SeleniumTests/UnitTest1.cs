
public class Tests
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using NUnit.Framework;
namespace SeleniumTests;
[TestFixture]
public abstract class TestBase
{
    protected IWebDriver _driver;

    [SetUp]
    public void Setup()	
    {
        // Указываем аргументы для ChromeDriver
        var options = new ChromeOptions();
        options.AddArgument("--headless");  // Запуск в headless-режиме
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");

        _driver = new ChromeDriver(options); // Создаем драйвер
    }

    [TearDown]
    public void TearDown()
    {
        _driver.Quit(); // Всегда завершаем работу драйвера
    }
}
