document.addEventListener("DOMContentLoaded", () => {
  if (!document.body.classList.contains("identity-login")) {
    return;
  }

  // Hide external provider panel on login.
  const externalPanel = document.querySelector(".col-lg-4.col-lg-offset-2");
  if (externalPanel) {
    externalPanel.remove();
  }

  // Expand local login panel to full width.
  const localPanel = document.querySelector(".col-lg-8");
  if (localPanel) {
    localPanel.classList.remove("col-lg-8");
    localPanel.classList.add("col-12");
  }

  // Registration is admin-only, so remove the public register link from login screen.
  const registerLinks = document.querySelectorAll("a[href*=\"/Identity/Account/Register\"]");
  registerLinks.forEach((link) => {
    const parent = link.closest("p");
    if (parent) {
      parent.remove();
      return;
    }

    link.remove();
  });
});
