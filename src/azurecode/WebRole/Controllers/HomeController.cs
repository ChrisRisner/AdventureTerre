using Orleans.Host;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;

namespace AdventureTerreWebRole.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Adventure Terre";

            if (!OrleansAzureClient.IsInitialized)
            {
                FileInfo clientConfigFile = AzureConfigUtils.ClientConfigFileLocation;
                if (!clientConfigFile.Exists)
                {
                    var configFilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/bin/") + "ClientConfiguration.xml";
                    OrleansAzureClient.Initialize(configFilePath);
                }
                else
                {
                    OrleansAzureClient.Initialize(clientConfigFile);
                }
            }

            return View();
        }
    }
}
