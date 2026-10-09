using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using MyStake.Models;
using Microsoft.EntityFrameworkCore;
using Titan.Repository;
using Titan.Repository.Database;

namespace MyStake.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWhitelabelDBStorage _dbStorage;
        public Global.Logging.ILogger _logger;

        public AccountController(
            IWhitelabelDBStorage dbStorage,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            Global.Logging.ILogger logger
        )
        {
            _dbStorage = dbStorage;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;

        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var ErrorContent_Email = "";
            var ErrorContent_Password = "";
            var ErrorContent_ConfirmPassword = "";
            var ErrorContent_Currency = "";

            var promoCode = string.IsNullOrWhiteSpace(model.promocode)
                ? "000000"
                : model.promocode.Trim();

            if (promoCode != "000000")
            {
                var agentExists = await _dbStorage.Context.Agents
                    .AnyAsync(a => a.PromoCode == promoCode);

                if (!agentExists)
                {
                    _logger.Error($"[AccountController->Register] [invalid promo code] promoCode: {promoCode}");

                    return Json(new
                    {
                        success = false,
                        msg = "Sign Up Failed!",
                        errorPromoCode = "Invalid promo code."
                    });
                }
            }


            var existUser = await _dbStorage.Context.Users
                .Where(s => s.UserEmail == model.Email)
                .FirstOrDefaultAsync();

            if (existUser != null)
            {
                _logger.Error($"[AccountController->Register] [user already exists] email: {model.Email}");

                ErrorContent_Email = "User already exists.";

                return Json(new
                {
                    success = false,
                    msg = "Sign Up Failed!",
                    errorEmail = ErrorContent_Email,
                    errorPassword = ErrorContent_Password,
                    errorConfirmPassword = ErrorContent_ConfirmPassword,
                    errorCurrency = ErrorContent_Currency
                });
            }


            // validation part remains unchanged


            if (!string.IsNullOrEmpty(ErrorContent_Email) ||
                !string.IsNullOrEmpty(ErrorContent_Password) ||
                !string.IsNullOrEmpty(ErrorContent_ConfirmPassword) ||
                !string.IsNullOrEmpty(ErrorContent_Currency))
            {
                _logger.Error($"[AccountController->Register] [validation failed] email: {model.Email}, emailError: {ErrorContent_Email}, passwordError: {ErrorContent_Password}, currencyError: {ErrorContent_Currency}");

                return Json(new
                {
                    success = false,
                    msg = "Sign Up Failed!",
                    errorEmail = ErrorContent_Email,
                    errorPassword = ErrorContent_Password,
                    errorConfirmPassword = ErrorContent_ConfirmPassword,
                    errorCurrency = ErrorContent_Currency
                });
            }


            var UserCode = GenerateRandomString();

            var identityUser = new IdentityUser
            {
                UserName = UserCode,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(
                identityUser,
                model.Password);


            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    if (error.Code.Contains("Duplicate"))
                    {
                        ErrorContent_Email = "User already exists.";
                    }
                    else if (error.Code.Contains("Password"))
                    {
                        ErrorContent_Password = error.Description;
                    }
                }

                _logger.Error($"[AccountController->Register] [identity user creation failed] email: {model.Email}, error: {string.Join(",", result.Errors.Select(x => x.Description))}");

                return Json(new
                {
                    success = false,
                    msg = "Sign Up Failed!",
                    errorEmail = ErrorContent_Email,
                    errorPassword = ErrorContent_Password,
                    errorConfirmPassword = ErrorContent_ConfirmPassword,
                    errorCurrency = ErrorContent_Currency
                });
            }


            var agent = await _dbStorage.Context.Agents
                .Where(s => s.PromoCode == promoCode)
                .FirstOrDefaultAsync();

            if (agent == null)
            {
                _logger.Error($"[AccountController->Register] [agent not found] promoCode: {promoCode}, email: {model.Email}");

                return Json(new
                {
                    success = false,
                    msg = "Upline agent not found."
                });
            }


            var user = new User
            {
                UserCode = UserCode,
                UserNickName = GenerateRandomString(),
                UserEmail = model.Email,
                CurrentBalance = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Password = model.Password,
                Status = 1,
                CurrencyId = 3,
                PromoCode = promoCode,
                CurrencyCode = "USD",
                AgentCode = agent.AgentCode,
                AgentPath = agent.AgentPath,
                AgentLevel = agent.AgentLevel,
                AgentTableId = agent.Id
            };


            try
            {
                _dbStorage.Context.Users.Add(user);

                await _dbStorage.Context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var message = ex.Message;

                if (ex.InnerException != null)
                {
                    message += " | Inner: " + ex.InnerException.Message;
                }

                _logger.Error($"[AccountController->Register] [database save failed] email: {model.Email}, error: {message}");

                throw;
            }


            await _signInManager.SignInAsync(
                identityUser,
                isPersistent: false);


            _logger.Error($"[AccountController->Register] [success] userCode: {UserCode}, email: {model.Email}, promoCode: {promoCode}");


            return Json(new
            {
                success = true,
                msg = "Successfully Signed Up!"
            });
        }

        [HttpPost]
        public async Task<IActionResult> DoLogin(LoginViewModel model)
        {
            var ErrorContent_Email = "";
            var ErrorContent_Password = "";

            if (string.IsNullOrEmpty(model.Email))
            {
                ErrorContent_Email = "Email is required.";
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                ErrorContent_Password = "Password is required.";
            }

            if (!string.IsNullOrEmpty(ErrorContent_Email) ||
                !string.IsNullOrEmpty(ErrorContent_Password))
            {
                _logger.Error($"[AccountController->DoLogin] [validation failed] email: {model.Email}, emailError: {ErrorContent_Email}, passwordError: {ErrorContent_Password}");

                return Json(new
                {
                    success = false,
                    errorEmail = ErrorContent_Email,
                    errorPassword = ErrorContent_Password,
                    msg = ""
                });
            }


            var identityUser = await _userManager.FindByEmailAsync(model.Email);

            if (identityUser == null)
            {
                _logger.Error($"[AccountController->DoLogin] [user not found] email: {model.Email}");

                return Json(new
                {
                    success = false,
                    errorEmail = "",
                    errorPassword = "",
                    msg = "Invalid Email or Incorrect password."
                });
            }


            var result = await _signInManager.PasswordSignInAsync(
                identityUser.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);


            if (result.Succeeded)
            {
                _logger.Error($"[AccountController->DoLogin] [success] email: {model.Email}, userCode: {identityUser.UserName}");

                return Json(new
                {
                    success = true,
                    msg = "Successfully Signed In!"
                });
            }
            else if (result.RequiresTwoFactor)
            {
                _logger.Error($"[AccountController->DoLogin] [two factor required] email: {model.Email}");

                return Json(new
                {
                    success = false,
                    requires2FA = true,
                    msg = "Two-Factor Authentication required."
                });
            }
            else if (result.IsLockedOut)
            {
                _logger.Error($"[AccountController->DoLogin] [account locked out] email: {model.Email}");

                return Json(new
                {
                    success = false,
                    msg = "Account locked out."
                });
            }
            else
            {
                _logger.Error($"[AccountController->DoLogin] [invalid password] email: {model.Email}");

                return Json(new
                {
                    success = false,
                    msg = "Invalid Email or Incorrect password."
                });
            }
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("", "Home");
        }

        private string GenerateRandomString(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();

            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }

        public async Task<IActionResult> GetUserInfo()
        {
            var player = await _dbStorage.Context.Users
                .Where(s => s.UserCode == User.Identity.Name)
                .FirstOrDefaultAsync();

            if (player == null)
            {
                _logger.Error($"[AccountController->GetUserInfo] [user not found] userCode: {User.Identity.Name}");

                return Json(new
                {
                    success = false
                });
            }

            _logger.Error($"[AccountController->GetUserInfo] [success] userCode: {User.Identity.Name}");

            return Json(new
            {
                success = true,
                player
            });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordModal model)
        {
            var ErrorCurrentPassword = "";
            var ErrorMatchPassword = "";

            var user = await _dbStorage.Context.Users
                .Where(s => s.UserCode == User.Identity.Name)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.Error($"[AccountController->ChangePassword] [user not found] userCode: {User.Identity.Name}");

                return Json(new
                {
                    success = false,
                    msg = "User not found."
                });
            }


            if (model.CurrentPassword != user.Password)
            {
                ErrorCurrentPassword = "Current password is incorrect";

                _logger.Error($"[AccountController->ChangePassword] [current password incorrect] userCode: {User.Identity.Name}");
            }
            else
            {
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ErrorMatchPassword = "Confirm Password doesn't match";

                    _logger.Error($"[AccountController->ChangePassword] [password mismatch] userCode: {User.Identity.Name}");
                }
                else
                {
                    user.Password = model.NewPassword;

                    await _dbStorage.Context.SaveChangesAsync();

                    var identityUser = await _userManager.GetUserAsync(User);

                    var result = await _userManager.ChangePasswordAsync(
                        identityUser,
                        model.CurrentPassword,
                        model.NewPassword);


                    if (result.Succeeded)
                    {
                        await _userManager.UpdateSecurityStampAsync(identityUser);

                        _logger.Error($"[AccountController->ChangePassword] [success] userCode: {User.Identity.Name}");

                        return Json(new
                        {
                            success = true
                        });
                    }
                    else
                    {
                        _logger.Error($"[AccountController->ChangePassword] [identity password change failed] userCode: {User.Identity.Name}, error: {string.Join(",", result.Errors.Select(x => x.Description))}");
                    }
                }
            }


            _logger.Error($"[AccountController->ChangePassword] [failed] userCode: {User.Identity.Name}");

            return Json(new
            {
                success = false,
                msg = "Sign Up Failed!",
                errorPassword = ErrorCurrentPassword,
                errorConfirmPassword = ErrorMatchPassword,
            });
        }

    }
}
