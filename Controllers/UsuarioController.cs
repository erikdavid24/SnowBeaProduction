using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BAEClassLibrary;

namespace SnowTrolleyProduction.Controllers
{
    public class UsuarioController : BAEController
    {
        // GET: Usuario
        public ActionResult Index()
        {
            return View();
        }
    }
}