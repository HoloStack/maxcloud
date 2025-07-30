using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using cloudDev.Models;
using cloudDev.Services;
using System.Security.Cryptography;
using System.Text;

namespace cloudDev.Controllers;

public class AccountController : Controller
{
    private readonly TableStorageService _tableStorageService;

    public AccountController(TableStorageService tableStorageService)
    {
        _tableStorageService = tableStorageService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var hashedPassword = HashPassword(model.Password);
        var isValidUser = await _tableStorageService.CheckPassword(model.Email, hashedPassword);

        if (isValidUser)
        {
            var customer = await _tableStorageService.GetCustomerByEmailAsync(model.Email);
            if (customer != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, customer.Name),
                    new Claim(ClaimTypes.Email, customer.Email),
                    new Claim("IsAdmin", customer.IsAdmin.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToAction("Index", "Home");
            }
        }

        ModelState.AddModelError("", "Invalid email or password");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check if user already exists
        var existingCustomer = await _tableStorageService.GetCustomerByEmailAsync(model.Email);
        if (existingCustomer != null)
        {
            ModelState.AddModelError("Email", "An account with this email already exists");
            return View(model);
        }

        var customer = new CustomerModel
        {
            PartitionKey = "Region1",
            RowKey = Guid.NewGuid().ToString(),
            Name = model.Name,
            Email = model.Email,
            PasswordHash = HashPassword(model.Password),
            IsAdmin = false // New users are not admin by default
        };

        try
        {
            await _tableStorageService.AddCustomerAsync(customer);
            TempData["Success"] = "Account created successfully! Please login.";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Error creating account: " + ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    [HttpGet]
    public IActionResult GoogleSignIn()
    {
        // Challenge the user with Google authentication
        var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleCallback") };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback()
    {
        // Get the external authentication result
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (authenticateResult.Succeeded)
        {
            var principal = authenticateResult.Principal;
            var email = principal.FindFirstValue(ClaimTypes.Email);
            var name = principal.FindFirstValue(ClaimTypes.Name);
            
            if (email == null || name == null)
            {
                TempData["Error"] = "Error getting user info from Google. Please try again.";
                return RedirectToAction("Login");
            }

            // Check if user exists in the database
            var customer = await _tableStorageService.GetCustomerByEmailAsync(email);
            if (customer == null)
            {
                // If user doesn't exist, create a new account
                customer = new CustomerModel
                {
                    PartitionKey = "Region1",
                    RowKey = Guid.NewGuid().ToString(),
                    Name = name,
                    Email = email,
                    PasswordHash = "", // No password for OAuth users
                    IsAdmin = false
                };
                await _tableStorageService.AddCustomerAsync(customer);
            }
            
            // Create claims for local authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, customer.Name),
                new Claim(ClaimTypes.Email, customer.Email),
                new Claim("IsAdmin", customer.IsAdmin.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            // Sign in the user locally
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), authProperties);

            TempData["Success"] = $"Welcome back, {name}!";
            return RedirectToAction("Index", "Home");
        }
        
        TempData["Error"] = "Google authentication failed. Please try again.";
        return RedirectToAction("Login");
    }
}
