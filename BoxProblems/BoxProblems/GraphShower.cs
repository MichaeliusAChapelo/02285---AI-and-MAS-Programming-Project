using BoxProblems.Graphing;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BoxProblems
{
    internal static class GraphShower
    {
        private static IWebDriver Browser = null;

        private static void Initialize()
        {
            Browser = new ChromeDriver(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webdrivers", "windows"));
            Browser.Navigate().GoToUrl(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webpage", "index.html"));
            Browser.Manage().Window.Maximize();
        }

        public static void ShowGraph<N, E>(Graph<N, E> graph)
        {
            if (Browser == null)
            {
                Initialize();
            }

            var graphInfo = graph.ToCytoscapeString();
            IJavaScriptExecutor jsExe = (IJavaScriptExecutor)Browser;
            jsExe.ExecuteScript($"setGraph({graphInfo.nodes}, {graphInfo.edges});");
        }

        public static void Shutdown()
        {
            Browser?.Quit();
            Browser = null;
        }
    }
}
