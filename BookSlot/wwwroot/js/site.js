// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
    const storageKey = "bookslot-theme";
    const root = document.documentElement;

    const getTheme = () => root.dataset.theme === "dark" ? "dark" : "light";

    const applyTheme = (theme) => {
        const normalized = theme === "dark" ? "dark" : "light";
        root.dataset.theme = normalized;
        localStorage.setItem(storageKey, normalized);

        document.querySelectorAll("[data-theme-icon]").forEach((icon) => {
            icon.className = normalized === "dark" ? "bi bi-moon-stars-fill" : "bi bi-sun-fill";
        });

        document.querySelectorAll("[data-theme-label]").forEach((label) => {
            label.textContent = normalized === "dark" ? "Темна тема" : "Світла тема";
        });

        document.querySelectorAll("[data-theme-toggle]").forEach((button) => {
            button.setAttribute("aria-pressed", normalized === "dark" ? "true" : "false");
        });
    };

    const initTheme = () => {
        applyTheme(getTheme());

        document.querySelectorAll("[data-theme-toggle]").forEach((button) => {
            button.addEventListener("click", () => {
                applyTheme(getTheme() === "dark" ? "light" : "dark");
            });
        });
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initTheme);
    } else {
        initTheme();
    }
})();
