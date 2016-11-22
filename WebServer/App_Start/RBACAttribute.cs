﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using WebServer.Models;
using System.Web.Mvc;

namespace WebServer.App_Start
{
    public class RBACAttribute : System.Web.Mvc.AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            /*Create hasPermission string based on the requested controller 
             name and action name in the format 'controllername-action'*/
            string requiredPermission = String.Format("{0}-{1}",
                   filterContext.ActionDescriptor.ControllerDescriptor.ControllerName,
                   filterContext.ActionDescriptor.ActionName);

            RBACUser requestingUser = new RBACUser(filterContext.RequestContext.HttpContext.User.Identity.Name);

            if (!requestingUser.HasPermission(requiredPermission))
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary{
                        {"action","Index"},
                        {"controller","Account"}
                    });
            }
            base.OnAuthorization(filterContext);
        }
    }
}