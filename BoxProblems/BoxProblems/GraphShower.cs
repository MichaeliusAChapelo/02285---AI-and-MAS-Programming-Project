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

        public static void ShowGraph<N, E>(Graph<N, E> graph)
        {
            ShowGraphs(new Graph<N, E>[] { graph });
        }

        public static void ShowGraphs<N, E>(Graph<N, E>[] graphs)
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

        public static void ShowSimplifiedGraph<N, E>(Graph<N, E> graph) where E : new()
        {
            ShowSimplifiedGraphs(new Graph<N, E>[] { graph });
        }

        public static void ShowSimplifiedGraphs<N, E>(Graph<N, E>[] graphs) where E : new()
        {
            ShowGraphs(graphs.Select(x => Graph<N, E>.CreateSimplifiedGraph(x)).ToArray());
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
