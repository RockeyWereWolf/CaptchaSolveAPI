using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace CaptchaSolverApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaptchaSolverController : ControllerBase
    {
        [HttpPost("solve")]
        public async Task<IActionResult> SolveCaptcha()
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            chromeOptions.AddArgument("--disable-gpu");
            chromeOptions.AddArgument("--no-sandbox");

            using var driver = new ChromeDriver(chromeOptions);

            try
            {
                driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument", new Dictionary<string, object>
                {
                    {"source", @"
                        Element.prototype._attachShadow = Element.prototype.attachShadow;
                        Element.prototype.attachShadow = function () {
                            return this._attachShadow({ mode: 'open' });
                        }"}
                });//js script to treat all shadow roots as open

                driver.Navigate().GoToUrl("https://nopecha.com/captcha/turnstile");

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                var shadowHost = wait.Until(d => d.FindElement(By.Id("parent")));

                var shadowRoot = shadowHost.FindElement(By.CssSelector("div#example-container5 > div")).GetShadowRoot();

                var iframe = shadowRoot.FindElement(By.CssSelector("iframe"));
                driver.SwitchTo().Frame(iframe);

                await Task.Delay(2000);

                var secondShadowHost = driver.FindElement(By.CssSelector("body"));

                var secondShadowRoot = secondShadowHost.GetShadowRoot();

                var checkbox = secondShadowRoot.FindElement(By.CssSelector("input"));
                checkbox.Click();

                var successMessage = secondShadowRoot.FindElement(By.Id("success-text"));
                if (successMessage.Displayed)
                    return Ok("Captcha solved successfully.");

                return BadRequest("Captcha solving failed.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
            finally
            {
                driver.Quit();
            }
        }
    }
}