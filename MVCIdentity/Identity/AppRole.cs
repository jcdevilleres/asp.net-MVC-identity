using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCIdentity.Identity
{
    public class AppRole : IdentityRole
    {
        public AppRole() : base () { }
        public AppRole(string name) : base(name) { }
    }
}