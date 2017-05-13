using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TurboSearch;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private static Search _fetcher = new Search();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }


        public ActionResult Query()
        {
            var resulsList = new List<WebPage>();
            foreach (var item in _fetcher.Ranker.UrlTitleFinalResults)
            {
                var page = new WebPage() { Title = item.Value, Url = item.Key, Id = 1 };
                resulsList.Add(page);
            }

            return View(resulsList);
        }

        [HttpPost]
        public ActionResult Query([Bind(Include = "InputQuery")]UserQuery model)
        {
            _fetcher.Query(model.InputQuery);

            return RedirectToAction("Query");
        }
    }
}