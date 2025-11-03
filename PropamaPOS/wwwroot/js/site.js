// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// spinner login
document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.getElementById('loginForm');

    if (loginForm) {
        loginForm.addEventListener('submit', function () {
            const btnLogin = document.getElementById('btnLogin');
            const loginText = document.getElementById('loginText');
            const loginSpinner = document.getElementById('loginSpinner');

            loginText.classList.add('d-none');
            loginSpinner.classList.remove('d-none');
            btnLogin.disabled = true;
        });
    }
});

document.addEventListener('DOMContentLoaded', function () {
    // Confirmación para eliminaciones
    const deleteButtons = document.querySelectorAll('.btn-delete-confirm');
    deleteButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            if (!confirm('¿Estás seguro de que quieres eliminar este registro? Esta acción no se puede deshacer.')) {
                e.preventDefault();
            }
        });
    });
});

document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".toggle-password").forEach(btn => {
        btn.addEventListener("click", function () {
            const input = this.previousElementSibling;
            const icon = this.querySelector("i");
            if (input.type === "password") {
                input.type = "text";
                icon.classList.remove("bi-eye");
                icon.classList.add("bi-eye-slash");
            } else {
                input.type = "password";
                icon.classList.remove("bi-eye-slash");
                icon.classList.add("bi-eye");
            }
        });
    });
});

// Sidebar toggle
document.addEventListener("DOMContentLoaded", function () {
    const sidebar = document.getElementById("appSidebar");
    const toggleBtn = document.getElementById("sidebarToggleBtn");

    // 🔹 Si no hay sidebar, quitar margen y salir
    if (!sidebar) {
        document.body.classList.remove("sidebar-hidden");
        document.body.classList.remove("preload");
        return;
    }

    if (!toggleBtn) return;

    // Leer estado guardado
    let hidden = localStorage.getItem("propama_sidebar_hidden");
    if (hidden === null) hidden = "false";
    hidden = hidden === "true";


    applySidebarState(hidden, true);

    // Quita la clase preload una vez aplicado el estado
    setTimeout(() => {
        document.body.classList.remove("preload");
    }, 0);


    toggleBtn.addEventListener("click", function () {
        hidden = !hidden;
        applySidebarState(hidden);
    });

    function applySidebarState(isHidden, instant = false) {
        const transition = instant ? "none" : "transform 0.35s cubic-bezier(0.4, 0, 0.2, 1)";
        if (instant) sidebar.style.transition = "none";
        sidebar.style.transition = transition;

        if (isHidden) {
            document.body.classList.add("sidebar-hidden");
            sidebar.classList.add("hidden");
        } else {
            document.body.classList.remove("sidebar-hidden");
            sidebar.classList.remove("hidden");
        }

        localStorage.setItem("propama_sidebar_hidden", isHidden);
    }
});


// === Guardar estado de secciones colapsables del sidebar ===
document.addEventListener("DOMContentLoaded", function () {
    const collapsibleLinks = document.querySelectorAll('.menu-section > a[data-bs-toggle="collapse"]');

    collapsibleLinks.forEach(link => {
        const targetId = link.getAttribute("href")?.replace("#", "");
        const target = document.getElementById(targetId);

        if (!target) return;

        // 🔹 Restaurar estado previo desde localStorage
        const savedState = localStorage.getItem("collapse_" + targetId);
        if (savedState === "true") {
            const collapse = new bootstrap.Collapse(target, { toggle: false });
            collapse.show();
            link.setAttribute("aria-expanded", "true");
        }

        // 🔹 Escuchar cambios (expandir / colapsar)
        target.addEventListener("shown.bs.collapse", () => {
            localStorage.setItem("collapse_" + targetId, "true");
        });
        target.addEventListener("hidden.bs.collapse", () => {
            localStorage.setItem("collapse_" + targetId, "false");
        });
    });
});



