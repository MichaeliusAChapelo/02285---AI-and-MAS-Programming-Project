using BoxProblems.Graphing;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BoxProblems
{
    internal static class GraphShower
    {
        private static IWebDriver Browser = null;
        private static Task CheckIfBrowserRunningTask = null;

        private static void Initialize()
        {
            Browser = new ChromeDriver(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webdrivers", "windows"));
            Browser.Navigate().GoToUrl(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webpage", "index.html"));
            Browser.Manage().Window.Maximize();

            CheckIfBrowserRunningTask = Task.Factory.StartNew(CheckIsBrowserClosed, TaskCreationOptions.LongRunning);
        }

        private async static void CheckIsBrowserClosed()
        {
            while (true)
            {
                try
                {
                    var _ = Browser.WindowHandles;
                }
                catch (Exception)
                {
                    Environment.Exit(0);
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }

        public static void ShowGraph(Graph graph)
        {
            ShowGraphs(new Graph[] { graph });
        }

        public static void ShowGraphs(Graph[] graphs)
        {
            if (Browser == null)
            {
                Initialize();
            }

            var graphsInfo = graphs.Select(x => x.ToCytoscapeString()).ToArray();
            string nodesString = string.Join(string.Empty, graphsInfo.Select(x => x.nodes));
            string edgesString = string.Join(string.Empty, graphsInfo.Select(x => x.edges));

            IJavaScriptExecutor jsExe = (IJavaScriptExecutor)Browser;
            string js = $"setGraph([{nodesString}], [{edgesString}]);";
            jsExe.ExecuteScript(js);
        }

        public static void ShowSimplifiedGraph<E>(Graph graph) where E : new()
        {
            ShowSimplifiedGraphs<E>(new Graph[] { graph });
        }

        public static void ShowSimplifiedGraphs<E>(Graph[] graphs) where E : new()
        {
            ShowGraphs(graphs.Select(x => Graph.CreateSimplifiedGraph<E>(x)).ToArray());
        }

        public static void Shutdown()
        {
            Browser?.Quit();
            Browser = null;

            CheckIfBrowserRunningTask?.Wait();
            CheckIfBrowserRunningTask?.Dispose();
            CheckIfBrowserRunningTask = null;
        }
    }
}
