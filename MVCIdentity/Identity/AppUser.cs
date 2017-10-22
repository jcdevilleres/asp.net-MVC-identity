using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCIdentity.Identity
{
    public class AppUser : IdentityUser
    {
        public string MyExtraProperty { get; set; }
    }
}