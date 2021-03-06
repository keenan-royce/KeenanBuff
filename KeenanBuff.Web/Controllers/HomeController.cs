﻿using KeenanBuff.Common.Logger.Interfaces;
using KeenanBuff.Entities.Context.Interfaces;
using KeenanBuff.Entities.SteamAPI.Interfaces;
using System.Web.Mvc;


namespace KeenanBuff.Controllers
{

    public class HomeController : Controller
    {
        private readonly IFileLogger _fileLogger;
        private readonly ISeedDatabase _seedDatabase;
        private readonly IKeenanBuffContext _context;

        public HomeController(IKeenanBuffContext context, IFileLogger fileLogger, ISeedDatabase seedDatabase)
        {
            _fileLogger = fileLogger;
            _seedDatabase = seedDatabase;
            _context = context;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Arcade()
        {
            return View();
        }

        public ActionResult Database(bool update = false, int matches = 0)
        {
            if (update)
            {
                _seedDatabase.Update(_context, matches);
            }

            return View();
        }

    }
}