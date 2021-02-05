﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace TwitchGrobs
{
    static class Helper
    {
        public static string GetUntilOrEmpty(this string text, string stopAt = "%")
        {
            if (!String.IsNullOrWhiteSpace(text))
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

                if (charLocation > 0)
                {
                    return text.Substring(0, charLocation);
                }
            }
            return String.Empty;
        }
    }

    class Program
    {
        static List<string> onlineList = new List<string>();
        static int currentOnline = 0;

        static void Main(string[] args)
        {
            foreach (var process in System.Diagnostics.Process.GetProcessesByName("chrome"))
                process.Kill();
            var options = new ChromeOptions();
            options.AddArgument("user-data-dir=C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Google\\Chrome\\User Data");
            options.AddArgument("--log-level=3");
            //ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            //service.SuppressInitialDiagnosticInformation = true;

            using (IWebDriver driver = new ChromeDriver(options))
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                int currentStreamer = 0;

                StreamerCheck(driver);

                while (true)
                {
                    if (currentStreamer < currentOnline)
                    {
                        driver.Navigate().GoToUrl("https://twitch.tv/" + onlineList[currentStreamer]);
                        
                        System.Threading.Thread.Sleep(5000);
                        try
                        {
                            driver.FindElement(By.XPath("/html/body/div[1]/div/div[2]/nav/div/div[3]/div[6]/div/div/div/div/button")).Click(); // Clicking on profile button to get % of drop
                            System.Threading.Thread.Sleep(1000);
                            var percent = driver.FindElement(By.XPath("/html/body/div[5]/div/div/div/div/div/div/div/div/div/div/div/div[3]/div/div/div[1]/div[9]/a/div/div[2]/p[2]")).GetAttribute("textContent");
                            var perName = percent.Substring(percent.LastIndexOf('/') + 1);
                            if (perName != onlineList[currentStreamer].ToLowerInvariant())
                            {
                                Console.WriteLine("Watching the wrong streamer. Switching...");
                                currentStreamer++;
                            }
                            else
                            {
                                Console.WriteLine("Currently watching " + onlineList[currentStreamer]);

                                Stopwatch sw = new Stopwatch();
                                sw.Start();
                                while (sw.Elapsed < TimeSpan.FromMinutes(15))
                                {
                                    System.Threading.Thread.Sleep(10); // reducing CPU use
                                    Console.Write("\rPercentage of drop {0}    " , driver.FindElement(By.XPath("/html/body/div[5]/div/div/div/div/div/div/div/div/div/div/div/div[3]/div/div/div[1]/div[9]/a/div/div[2]/p[2]")).GetAttribute("textContent").GetUntilOrEmpty());
                                    if (percent.GetUntilOrEmpty() == "100")
                                    {
                                        Console.WriteLine("100% on one of drops. Claiming and switching streamer.");
                                        ClaimDrop(driver);
                                        currentStreamer++;
                                        break;
                                    }
                                }

                                StreamerCheck(driver);
                            }
                        }
                        catch
                        {
                            Console.WriteLine("No drops progression... Switching streamer in a minute.");
                            currentStreamer++;
                            System.Threading.Thread.Sleep(60000);
                        }
                    }
                    else
                    {
                        currentStreamer = 0;
                    }
                    System.Threading.Thread.Sleep(10); // Less CPU usage
                }
            }
        }

        static void ClaimDrop(IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://www.twitch.tv/drops/inventory");
            System.Threading.Thread.Sleep(5000);

            driver.FindElement(By.XPath("//button[@data-test-selector ='DropsCampaignInProgressRewardPresentation-claim-button']")).Click();
        }

        static void StreamerCheck(IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://twitch.facepunch.com/");
            currentOnline = 0;
            onlineList.Clear();
            System.Threading.Thread.Sleep(5000);
            for (int x = 1; x <= 3; x++)
            {
                for (int y = 2; y <= 4; y++)
                {
                    string streamerHeader = $"/html/body/div[1]/div[2]/div[{y}]/a[{x}]/div[1]";
                    var streamerName = driver.FindElement(By.XPath(streamerHeader)).FindElement(By.ClassName("drop-item__header-streamer")).FindElement(By.ClassName("username"));

                    //a = a.FindElement(By.ClassName("drop-item__header-streamer"));
                    //a = a.FindElement(By.ClassName("username"));

                    string statusHeader = $"/html/body/div[1]/div[2]/div[{y}]/a[{x}]/div[1]";
                    var status = driver.FindElement(By.XPath(statusHeader)).FindElement(By.ClassName("drop-item__header-status")).GetAttribute("textContent");
                    status = string.Join("", status.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

                    if (status == "Live")
                    {
                        onlineList.Add(streamerName.GetAttribute("textContent"));
                        currentOnline++;
                    }
                }
            }
            Console.Clear();
            foreach (var a in onlineList)
                Console.WriteLine(a + " is live.");
        }
    }
}
