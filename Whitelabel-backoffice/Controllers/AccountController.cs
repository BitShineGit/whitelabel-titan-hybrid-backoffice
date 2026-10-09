using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using Whitelabel_backoffice.Database;
using Whitelabel_backoffice.Models;
using Whitelabel_backoffice.Services.Interfaces;

namespace Whitelabel_backoffice.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        private readonly IGameService _gameService;
        private readonly AppSettings _appSetting;
        private readonly ILogger<AccountController> _logger;


        public AccountController(
            IStringLocalizer<SharedResource> lang,
            ILogger<AccountController> logger,
            IWhitelabelDBStorage dbStorage,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            IGameService gameService,
            IOptions<AppSettings> appsettings
        ) 
        {
            _logger = logger;
            _dbStorage = dbStorage;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _gameService = gameService;
            _appSetting = appsettings.Value;
        }


        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl ?? "/";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var ErrorContent_LoginName = "";
            var ErrorContent_Password = "";

            if (ModelState.IsValid)
            {
                var user = await _dbStorage.Context.Agents.Where(u => u.AgentLoginName == model.Username).FirstOrDefaultAsync();
                if (user == null)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Invalid Email or Incorrect password."
                    });
                }

                var identityUser = await _userManager.FindByNameAsync(user.AgentCode);
                if (identityUser == null)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Invalid Email or Incorrect password."
                    });
                }

                // Attempt sign-in
                var result =
                    await _signInManager.PasswordSignInAsync(
                        identityUser.UserName,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: true
                    );

                if (result.Succeeded)
                {
                    return Json(new
                    {
                        success = true,
                        msg = "Successfully Signed In!"
                    });
                }
                else if (result.RequiresTwoFactor)
                {
                    return Json(new
                    {
                        success = false,
                        requiresTwoFactor = true,
                        msg = "Two-factor authentication is required."
                    });
                }
                else if (result.IsLockedOut)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Account locked out."
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Invalid Email or Incorrect password."
                    });
                }
            }
            else
            {
                if (string.IsNullOrEmpty(model.Username))
                {
                    ErrorContent_LoginName = "LoginName is required.";
                }

                if (string.IsNullOrEmpty(model.Password))
                {
                    ErrorContent_Password = "Password is required.";
                }
            }

            return Json(new
            {
                success = false,
                errorLoginName = ErrorContent_LoginName,
                errorPassword = ErrorContent_Password
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }

        // =========================================================
        // ACCOUNT PAGE
        // =========================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {

                var identityName = User.Identity?.Name;


                Agent? agent = null;


                if (!string.IsNullOrEmpty(identityName))
                {

                    // First check Agent Login Name
                    agent =
                        await _dbStorage.Context.Agents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.AgentLoginName == identityName
                        );


                    // If not found, check Agent Code
                    if (agent == null)
                    {

                        agent =
                            await _dbStorage.Context.Agents
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.AgentCode == identityName
                            );

                    }

                }



                // Safety:
                // Do not redirect to login here.
                // The login middleware already handles authentication.

                if (agent == null)
                {

                    ViewData["Agent"] = new Agent();

                    ViewData["Currency"] = new Currency();

                    return View();

                }





                Currency? currency =
                    await _dbStorage.Context.Currencies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == agent.CurrencyId
                    );


                // =========================================================
                // IDENTITY USER
                // =========================================================

                var identityUser =
                    await _userManager.FindByNameAsync(agent.AgentCode);


                // =========================================================
                // VIEW DATA
                // =========================================================

                ViewData["Agent"] = agent;

                ViewData["Currency"] = currency;

                ViewData["IdentityUser"] = identityUser;


                return View();

            }
            catch (Exception ex)
            {

                _logger.LogError(
                    ex,
                    "Error loading account page."
                );


                ViewData["Agent"] = new Agent();

                ViewData["Currency"] = new Currency();


                return View();

            }

        }

        // =========================================================
        // SETUP TWO-FACTOR AUTHENTICATION
        // =========================================================

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SetupTwoFactor()
        {
            try
            {
                // =====================================================
                // FIND CURRENT AGENT
                // =====================================================

                var identityName =
                    User.Identity?.Name;

                if (string.IsNullOrEmpty(identityName))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "User is not authenticated."
                    });
                }


                var agent =
                    await _dbStorage.Context.Agents
                    .FirstOrDefaultAsync(x =>
                        x.AgentLoginName == identityName ||
                        x.AgentCode == identityName
                    );


                if (agent == null)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Account not found."
                    });
                }


                // =====================================================
                // FIND IDENTITY USER
                // =====================================================

                var identityUser =
                    await _userManager.FindByNameAsync(
                        agent.AgentCode
                    );


                if (identityUser == null)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Identity account not found."
                    });
                }


                // =====================================================
                // CHECK CURRENT STATUS
                // =====================================================

                if (identityUser.TwoFactorEnabled)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Two-factor authentication is already enabled."
                    });
                }


                // Generate a NEW authenticator key every time 2FA setup is started.
                var resetResult =
                    await _userManager.ResetAuthenticatorKeyAsync(identityUser);

                if (!resetResult.Succeeded)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Unable to generate authenticator key."
                    });
                }

                var secretKey =
                    await _userManager.GetAuthenticatorKeyAsync(identityUser);

                if (string.IsNullOrEmpty(secretKey))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Unable to retrieve authenticator key."
                    });
                }


                // =====================================================
                // DOMAIN
                // =====================================================

                var domain =
                    Request.Host.Host;


                if (string.IsNullOrWhiteSpace(domain))
                {
                    domain = "Romaspin";
                }


                // =====================================================
                // AUTHENTICATOR ACCOUNT
                // =====================================================

                var accountName =
                    $"{domain}:{agent.AgentCode}";


                // =====================================================
                // ISSUER
                // =====================================================

                var issuer =
                    domain;


                // =====================================================
                // OTP AUTH URI
                // =====================================================

                var otpauthUri =
                    $"otpauth://totp/" +
                    $"{Uri.EscapeDataString(accountName)}" +
                    $"?secret={Uri.EscapeDataString(secretKey)}" +
                    $"&issuer={Uri.EscapeDataString(issuer)}";


                // =====================================================
                // GENERATE QR CODE
                // =====================================================

                using var qrGenerator =
                    new QRCoder.QRCodeGenerator();


                using var qrData =
                    qrGenerator.CreateQrCode(
                        otpauthUri,
                        QRCoder.QRCodeGenerator.ECCLevel.Q
                    );


                var pngQrCode =
                    new QRCoder.PngByteQRCode(qrData);


                var qrCodeBytes =
                    pngQrCode.GetGraphic(10);


                var qrCodeBase64 =
                    Convert.ToBase64String(
                        qrCodeBytes
                    );


                // =====================================================
                // RETURN SETUP DATA
                // =====================================================

                return Json(new
                {
                    success = true,

                    qrCode =
                        $"data:image/png;base64,{qrCodeBase64}",

                    secret = secretKey,

                    accountName = accountName,

                    issuer = issuer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error setting up two-factor authentication."
                );


                return Json(new
                {
                    success = false,
                    msg = "Unable to setup two-factor authentication."
                });
            }
        }

        // =========================================================
        // ENABLE TWO-FACTOR AUTHENTICATION
        // =========================================================

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EnableTwoFactor(
            string code)
        {
            try
            {
                // =====================================================
                // VALIDATE CODE
                // =====================================================

                if (string.IsNullOrWhiteSpace(code))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Please enter the authentication code."
                    });
                }


                code =
                    code.Replace(" ", "")
                        .Replace("-", "");


                if (code.Length != 6 ||
                    !code.All(char.IsDigit))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Please enter a valid 6-digit code."
                    });
                }


                // =====================================================
                // FIND CURRENT AGENT
                // =====================================================

                var identityName =
                    User.Identity?.Name;


                if (string.IsNullOrEmpty(identityName))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "User is not authenticated."
                    });
                }


                var agent =
                    await _dbStorage.Context.Agents
                    .FirstOrDefaultAsync(x =>
                        x.AgentLoginName == identityName ||
                        x.AgentCode == identityName
                    );


                if (agent == null)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Account not found."
                    });
                }


                // =====================================================
                // FIND IDENTITY USER
                // =====================================================

                var identityUser =
                    await _userManager.FindByNameAsync(
                        agent.AgentCode
                    );


                if (identityUser == null)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Identity account not found."
                    });
                }


                // =====================================================
                // VERIFY AUTHENTICATOR CODE
                // =====================================================

                var isValid =
                    await _userManager.VerifyTwoFactorTokenAsync(
                        identityUser,
                        _userManager.Options.Tokens.AuthenticatorTokenProvider,
                        code
                    );


                if (!isValid)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Invalid authentication code."
                    });
                }


                // =====================================================
                // ENABLE TWO-FACTOR AUTHENTICATION
                // =====================================================

                var result =
                    await _userManager.SetTwoFactorEnabledAsync(
                        identityUser,
                        true
                    );


                if (!result.Succeeded)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Unable to enable two-factor authentication."
                    });
                }


                return Json(new
                {
                    success = true,
                    msg = "Two-factor authentication has been enabled."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error enabling two-factor authentication."
                );


                return Json(new
                {
                    success = false,
                    msg = "System error."
                });
            }
        }

        // =========================================================
        // DISABLE TWO-FACTOR AUTHENTICATION
        // =========================================================

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DisableTwoFactor(string code)
        {
            try
            {
                // =====================================================
                // VALIDATE CODE
                // =====================================================

                if (string.IsNullOrWhiteSpace(code))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Please enter the authentication code."
                    });
                }

                code = code.Replace(" ", "")
                           .Replace("-", "");

                if (code.Length != 6 || !code.All(char.IsDigit))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Please enter a valid 6-digit code."
                    });
                }


                // =====================================================
                // FIND CURRENT USER
                // =====================================================

                var identityName = User.Identity?.Name;

                if (string.IsNullOrEmpty(identityName))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "User is not authenticated."
                    });
                }


                // =====================================================
                // FIND AGENT
                // =====================================================

                var agent = await _dbStorage.Context.Agents
                    .FirstOrDefaultAsync(x =>
                        x.AgentLoginName == identityName ||
                        x.AgentCode == identityName);

                if (agent == null)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Account not found."
                    });
                }


                // =====================================================
                // FIND IDENTITY USER
                // =====================================================

                var identityUser =
                    await _userManager.FindByNameAsync(agent.AgentCode);

                if (identityUser == null)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Identity account not found."
                    });
                }


                // =====================================================
                // CHECK CURRENT STATUS
                // =====================================================

                if (!identityUser.TwoFactorEnabled)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Two-factor authentication is already disabled."
                    });
                }


                // =====================================================
                // VERIFY AUTHENTICATOR CODE
                // =====================================================

                var isValid =
                    await _userManager.VerifyTwoFactorTokenAsync(
                        identityUser,
                        _userManager.Options.Tokens.AuthenticatorTokenProvider,
                        code);

                if (!isValid)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Invalid authentication code."
                    });
                }


                // =====================================================
                // DISABLE TWO-FACTOR AUTHENTICATION
                // =====================================================

                var result =
                    await _userManager.SetTwoFactorEnabledAsync(
                        identityUser,
                        false);

                if (!result.Succeeded)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Unable to disable two-factor authentication."
                    });
                }


                // =====================================================
                // SUCCESS
                // =====================================================

                return Json(new
                {
                    success = true,
                    msg = "Two-factor authentication has been disabled."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error disabling two-factor authentication.");

                return Json(new
                {
                    success = false,
                    msg = "System error."
                });
            }
        }

        // =========================================================
        // CHANGE PASSWORD
        // =========================================================


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordModal model)
        {

            string errorCurrentPassword = "";

            string errorConfirmPassword = "";

            try
            {
                var userAgent =
                    await _dbStorage.Context.Agents
                    .FirstOrDefaultAsync(
                        x => x.AgentCode == User.Identity.Name
                    );



                if (userAgent == null)
                {

                    return Json(new
                    {

                        success = false,

                        msg = "Account not found."

                    });

                }

                // =================================================
                // CHECK CURRENT PASSWORD
                // =================================================


                if (model.CurrentPassword
                    != userAgent.Password)
                {
                    errorCurrentPassword =
                        "Current password is incorrect.";
                }

                else
                {

                    // =============================================
                    // CHECK CONFIRM PASSWORD
                    // =============================================


                    if (model.NewPassword
                        != model.ConfirmPassword)
                    {


                        errorConfirmPassword =
                            "Confirm Password doesn't match.";

                    }

                    else
                    {

                        // =============================================
                        // UPDATE DATABASE PASSWORD
                        // =============================================


                        userAgent.Password =
                            model.NewPassword;

                        await _dbStorage.Context.SaveChangesAsync();

                        // =============================================
                        // UPDATE IDENTITY PASSWORD
                        // =============================================


                        var identityUser =
                            await _userManager.GetUserAsync(User);



                        if (identityUser != null)
                        {


                            var result =
                                await _userManager.ChangePasswordAsync(
                                    identityUser,
                                    model.CurrentPassword,
                                    model.NewPassword
                                );



                            if (result.Succeeded)
                            {


                                await _userManager
                                    .UpdateSecurityStampAsync(identityUser);



                                return Json(new
                                {

                                    success = true

                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {


                _logger.LogError(
                    ex,
                    "Change password error."
                );



                return Json(new
                {

                    success = false,

                    msg = "System error."

                });


            }





            return Json(new
            {

                success = false,


                msg = "Change password failed.",


                errorPassword =
                    errorCurrentPassword,


                errorConfirmPassword =
                    errorConfirmPassword


            });


        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> VerifyTwoFactor(
            string code,
            bool rememberMe = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Please enter your authentication code."
                    });
                }

                code = code.Replace(" ", "")
                           .Replace("-", "");

                if (code.Length != 6 ||
                    !code.All(char.IsDigit))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Please enter a valid 6-digit code."
                    });
                }

                var result =
                    await _signInManager.TwoFactorAuthenticatorSignInAsync(
                        code: code,
                        isPersistent: rememberMe,
                        rememberClient: false
                    );

                if (result.Succeeded)
                {
                    return Json(new
                    {
                        success = true,
                        msg = "Successfully Signed In!"
                    });
                }

                if (result.IsLockedOut)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Account locked out."
                    });
                }

                return Json(new
                {
                    success = false,
                    msg = "Invalid authentication code."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Two-factor authentication verification error."
                );

                return Json(new
                {
                    success = false,
                    msg = "System error."
                });
            }
        }

        // =========================================================
        // UPDATE TIME ZONE
        // =========================================================

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateTimeZone(string timeZone)
        {
            try
            {
                // =====================================================
                // VALIDATE
                // =====================================================

                if (string.IsNullOrWhiteSpace(timeZone))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Please select a time zone."
                    });
                }


                // =====================================================
                // CURRENT USER
                // =====================================================

                var identityName =
                    User.Identity?.Name;


                if (string.IsNullOrEmpty(identityName))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Unable to identify the current agent."
                    });
                }


                // =====================================================
                // FIND AGENT
                // =====================================================

                Agent? agent =
                    await _dbStorage.Context.Agents
                        .FirstOrDefaultAsync(x =>
                            x.AgentCode == identityName
                        );


                // =====================================================
                // FALLBACK: AGENT CODE
                // =====================================================

                if (agent == null)
                {
                    agent =
                        await _dbStorage.Context.Agents
                            .FirstOrDefaultAsync(x =>
                                x.AgentCode == identityName
                            );
                }


                // =====================================================
                // AGENT NOT FOUND
                // =====================================================

                if (agent == null)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Agent not found."
                    });
                }


                // =====================================================
                // UPDATE TIME ZONE
                // =====================================================

                if (!Regex.IsMatch(
                        timeZone.Trim(),
                        @"^[+-](0\d|1[0-4]):[0-5]\d$"))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Invalid time zone format."
                    });
                }


                agent.TimeZone = timeZone.Trim();


                // =====================================================
                // SAVE
                // =====================================================

                await _dbStorage.Context.SaveChangesAsync();


                // =====================================================
                // SUCCESS
                // =====================================================

                return Json(new
                {
                    success = true
                });

            }
            catch (Exception ex)
            {

                _logger.LogError(
                    ex,
                    "Error updating agent time zone."
                );


                return Json(new
                {
                    success = false,
                    msg = "An error occurred while updating the time zone."
                });

            }
        }
    }
}