using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SinglySamplePhotos.Controllers
{
    public class MyPhotosController : Controller
    {
        //
        // GET: /MyPhotos/

        public ActionResult Index(string search = "")
        {
            var context = new Singly.SinglyContext();
            var photos = context.Photos
                                .Where(photo => photo.Data.Contains(search) && photo.Date < DateTime.Now.AddDays(-2)) 
                                .Take(10).ToList();

            return View(photos);
        }

    }
}
