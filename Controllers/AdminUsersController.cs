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
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? "-",
                Name = user.UserName ?? "-",
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

    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        var model = new AdminUserEditViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Role = role
        };

        await LoadRolesAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminUserEditViewModel model)
    {
        await LoadRolesAsync();

        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var normalizedUserName = model.UserName.Trim();

        var emailOwner = await userManager.FindByEmailAsync(normalizedEmail);
        if (emailOwner is not null && emailOwner.Id != user.Id)
        {
            ModelState.AddModelError(nameof(model.Email), "Another user already uses this email.");
            return View(model);
        }

        var userNameOwner = await userManager.FindByNameAsync(normalizedUserName);
        if (userNameOwner is not null && userNameOwner.Id != user.Id)
        {
            ModelState.AddModelError(nameof(model.UserName), "Another user already uses this name.");
            return View(model);
        }

        if (!await roleManager.RoleExistsAsync(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Selected role does not exist.");
            return View(model);
        }

        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await userManager.SetEmailAsync(user, normalizedEmail);
            if (!emailResult.Succeeded)
            {
                foreach (var error in emailResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
        }

        if (!string.Equals(user.UserName, normalizedUserName, StringComparison.Ordinal))
        {
            var nameResult = await userManager.SetUserNameAsync(user, normalizedUserName);
            if (!nameResult.Succeeded)
            {
                foreach (var error in nameResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(model.Role, StringComparer.Ordinal))
        {
            if (currentRoles.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    foreach (var error in removeResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(model);
                }
            }

            var addRoleResult = await userManager.AddToRoleAsync(user, model.Role);
            if (!addRoleResult.Succeeded)
            {
                foreach (var error in addRoleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
        }

        TempData["Success"] = $"User {normalizedUserName} updated.";
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
