using Cartrade.Data;
using Cartrade.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cartrade.Controllers;

[Authorize(Roles = SeedData.AdminRole)]
public class AdminUsersController(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var users = await userManager.Users
            .OrderBy(u => u.Email)
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        var items = new List<AdminUserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            items.Add(new AdminUserListItemViewModel
            {
                Email = user.Email ?? user.UserName ?? "-",
                Roles = roles.Count > 0 ? string.Join(", ", roles.OrderBy(r => r)) : "(none)",
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > now
            });
        }

        return View(items);
    }

    public async Task<IActionResult> Create()
    {
        await LoadRolesAsync();
        return View(new AdminUserCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminUserCreateViewModel model)
    {
        await LoadRolesAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var existing = await userManager.FindByEmailAsync(normalizedEmail);
        if (existing is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
            return View(model);
        }

        if (!await roleManager.RoleExistsAsync(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Selected role does not exist.");
            return View(model);
        }

        var user = new IdentityUser
        {
            Email = normalizedEmail,
            UserName = normalizedEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var roleResult = await userManager.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["Success"] = $"User {normalizedEmail} was created with role {model.Role}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadRolesAsync()
    {
        ViewData["Roles"] = await roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => r.Name!)
            .ToListAsync();
    }
}
