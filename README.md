# A sample ASP.NET MVC project with Authentication using ASP.NET Identity

This sample asp.net MVC project shows a sample on how to enable a single/master password protection for your website.
Initially, you will have to register a user and password combination. Afterwards, you only need to enter the specific password each time you access your website.
This is especially useful when securing a website against public use, e.g., you only want clients who has the password/token to be able to access your site.

Using ASP.NET Identity, here is bare minimum steps to enable Authentication to work:

## Under App_Start, create IdentityConfig class which defines the ApplicationUserManager and ApplicationSignInManager. 

  The ApplicationUserManager inherits from ASP.NET Identity UserManager which takes care of the primary key and modeling of your users (ApplicationUser : IdentityUser).

  The ApplicationSignInManager inherits from SignInManager which takes care of the SignIn operations of your users, this includes Two Factor SignIn, Password SignIn, External SignIn. It also supports asynchronous operations

*IdentityConfig.cs*

        public class ApplicationUserManager : UserManager<ApplicationUser>
            {
                public ApplicationUserManager(IUserStore<ApplicationUser> store)
                    : base(store)
                {
                }

                public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
                {
                    var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));
                    return manager;
                }
            }

            public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
            {
                public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
                    : base(userManager, authenticationManager)
                {
                }

                public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
                {
                    return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
                }

                public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
                {
                    return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
                }
            }

## Defined a Startup class for OWIN configuration, e.g., enable cookie authentication, external sign in cookie, and others.

  Note that this is a required class if you run into some issues, add an annotation to the class same as below and add a key/value on the web.config under appSettings:
         
*Web.config*    

        <appSettings>
        <add key="owin:AppStartup" value="MVCIdentity.App_Start.Startup" />
        ...
        </appSettings>

*App_Start/Startup.cs*

        [assembly: OwinStartup(typeof(MVCIdentity.App_Start.Startup))]
        namespace MVCIdentity.App_Start
        {
            public class Startup
          {
                public void Configuration(IAppBuilder app)
                {
                    app.CreatePerOwinContext(ApplicationDbContext.Create);
                    app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
                    app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                        LoginPath = new PathString("/Account/Login")                
                    });
                    app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
                }
          }
        }    

## Add methods for Login, Logout, and register under Account Controller

You will quickly notice that there are two implementations of the same method. The first one is publicly accessible without Authentication, you are redirected to this action initially.

Afterwards, when you click on the form buttons with action = POST you are directed to HttpPost methods, the view model is checked.

*AccountController, Login method*

        // When User is not logged in yet, he is redirected to the method below
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // Action performed when Login button clicked (form action = POST)
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        //Attribute below to prevent forgery of a request
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            //Checks if the model state using class LoginViewModel is valid
            //if invalid direct to Login page
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            // Check the login fields, afterwards directed to different actions below (switch case)
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);

            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

*AccountController, Register method*

       
        // Same 
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

*Account Controller, Log off method, this is obviously the hardest to code :) *

       // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }
        
## You will also need to create models, LoginViewModel and RegisterViewModel, for Login and Register

*AccountViewModel.cs*

The .NET Identity class already has default properties (Email, PasswordHash, and others), this can be viewed on the LocalDB which will be created after you registered a user.

Define this properies for your view, as shown below:
        
//The Login view uses the LoginViewModel to display the page 
   
   public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        //Default user email
        public LoginViewModel()
        {
            Email = "user@user.com";
        }
    }

    //If registration is needed, the Register view uses the RegisterViewModel to get relevant ApplicationUser details. Password are hashed in storage
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

## Do not forget to create Views for each action as well.

*Login.cshtml*

        @using MVCIdentity.Models
        @model LoginViewModel
        @{
            ViewBag.Title = "Log in";
        }

        <h2>@ViewBag.Title.</h2>
        <div class="row">
            <div class="col-md-8">
                <section id="loginForm">
                    @using (Html.BeginForm("Login", "Account", new { ReturnUrl = ViewBag.ReturnUrl }, FormMethod.Post, new { @class = "form-horizontal", role = "form" }))
                    {
                        @Html.AntiForgeryToken()
                        @*<h4>Use a local account to log in.</h4>*@
                        <h4>Enter password to enter website.</h4>
                        <hr />
                        @Html.ValidationSummary(true, "", new { @class = "text-danger" })
                        @*<div class="form-group">
                            @Html.LabelFor(m => m.Email, new { @class = "col-md-2 control-label" })
                            <div class="col-md-10">
                                @Html.TextBoxFor(m => m.Email, new { @class = "form-control" })
                                @Html.ValidationMessageFor(m => m.Email, "", new { @class = "text-danger" })
                            </div>
                        </div>*@
                        <div class="form-group">
                            @Html.LabelFor(m => m.Password, new { @class = "col-md-2 control-label" })
                            <div class="col-md-10">
                                @Html.PasswordFor(m => m.Password, new { @class = "form-control" })
                                @Html.ValidationMessageFor(m => m.Password, "", new { @class = "text-danger" })
                            </div>
                        </div>
                        <div class="form-group">
                            <div class="col-md-offset-2 col-md-10">
                                <div class="checkbox">
                                    @Html.CheckBoxFor(m => m.RememberMe)
                                    @Html.LabelFor(m => m.RememberMe)
                                </div>
                            </div>
                        </div>
                        <div class="form-group">
                            <div class="col-md-offset-2 col-md-10">
                                <input type="submit" value="Log in" class="btn btn-default" />
                            </div>
                        </div>
                        <p>
                            @Html.ActionLink("Register as a new user", "Register")
                        </p>
                        @* Enable this once you have account confirmation enabled for password reset functionality
                            <p>
                                @Html.ActionLink("Forgot your password?", "ForgotPassword")
                            </p>*@
                    }
                </section>
            </div>
            @*<div class="col-md-4">
                <section id="socialLoginForm">
                    @Html.Partial("_ExternalLoginsListPartial", new ExternalLoginListViewModel { ReturnUrl = ViewBag.ReturnUrl })
                </section>
            </div>*@
        </div>

        @section Scripts {
            @Scripts.Render("~/bundles/jqueryval")
        }
        
*Register.cshtml*

        @model MVCIdentity.Models.RegisterViewModel
        @{
            ViewBag.Title = "Register";
        }

        <h2>@ViewBag.Title.</h2>

        @using (Html.BeginForm("Register", "Account", FormMethod.Post, new { @class = "form-horizontal", role = "form" }))
        {
            @Html.AntiForgeryToken()
            <h4>Create a new account.</h4>
            <hr />
            @Html.ValidationSummary("", new { @class = "text-danger" })
            <div class="form-group">
                @Html.LabelFor(m => m.Email, new { @class = "col-md-2 control-label" })
                <div class="col-md-10">
                    @Html.TextBoxFor(m => m.Email, new { @class = "form-control" })
                </div>
            </div>
            <div class="form-group">
                @Html.LabelFor(m => m.Password, new { @class = "col-md-2 control-label" })
                <div class="col-md-10">
                    @Html.PasswordFor(m => m.Password, new { @class = "form-control" })
                </div>
            </div>
            <div class="form-group">
                @Html.LabelFor(m => m.ConfirmPassword, new { @class = "col-md-2 control-label" })
                <div class="col-md-10">
                    @Html.PasswordFor(m => m.ConfirmPassword, new { @class = "form-control" })
                </div>
            </div>
            <div class="form-group">
                <div class="col-md-offset-2 col-md-10">
                    <input type="submit" class="btn btn-default" value="Register" />
                </div>
            </div>
        }

        @section Scripts {
            @Scripts.Render("~/bundles/jqueryval")
        }


## We also have to initialize our AspNetIdentity Database and ApplicationUser Model. 

The ApplicationUser inherits from the IdentityUser. We can defined the ApplicationDbContext connection string here as well.

For advanced customization, you can override some methods to defined your Db, e.g., "protected override void OnModelCreating(DbModelBuilder modelBuilder)

*IndentityModel.cs*

        public class ApplicationUser : IdentityUser
            {
                public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
                {
                    var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
                    return userIdentity;
                }

            }

            public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
            {
                //IdentityConnection defines the LocalDB in the web.config connection string tag
                public ApplicationDbContext() : base("IdentityConnection", throwIfV1Schema: false)
                {

                }

                public static ApplicationDbContext Create()
                {
                    return new ApplicationDbContext();
                }

                //protected override void OnModelCreating(DbModelBuilder modelBuilder)
                //{
                //    base.OnModelCreating(modelBuilder); // I had removed this
                //                                        /// Rest of on model creating here.
                //}
            }

*The Visual Studio application has various template with different Authentication methods (Individual, Active Directory, OAuth, etc) which are good references to use*
