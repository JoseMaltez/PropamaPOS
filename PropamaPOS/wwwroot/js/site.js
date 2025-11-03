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

// Sidebar
(function () {
    const sidebar = document.getElementById('appSidebar');
    const btn = document.getElementById('sidebarCollapseBtn');
    if (!sidebar || !btn) return;

    // --- Estado inicial ---
    let isCollapsed = localStorage.getItem('propama_sidebar_collapsed') === 'true';
    applySidebarState(isCollapsed, true); 

    // --- Botón principal ---
    btn.addEventListener('click', function (e) {
        e.preventDefault();
        isCollapsed = !isCollapsed;
        applySidebarState(isCollapsed);
    });

    // animación 
    function applySidebarState(collapsed, instant = false) {
        const transition = instant ? "none" : "width 0.35s cubic-bezier(0.4, 0, 0.2, 1)";
        const contentTransition = instant ? "none" : "margin-left 0.35s cubic-bezier(0.4, 0, 0.2, 1)";
        const sidebarHeader = sidebar.querySelector(".sidebar-header");

        sidebar.style.transition = transition;
        document.querySelector(".main-column").style.transition = contentTransition;

        if (collapsed) {
            sidebar.classList.add("collapsed");
            document.body.classList.add("sidebar-collapsed");
            btn.innerHTML = '<i class="bi bi-chevron-right"></i>';

            // Cerrar submenús abiertos
            sidebar.querySelectorAll(".collapse.show").forEach(menu => {
                const bsCollapse = bootstrap.Collapse.getOrCreateInstance(menu);
                bsCollapse.hide();
            });
        } else {
            requestAnimationFrame(() => {
                sidebar.classList.remove("collapsed");
                document.body.classList.remove("sidebar-collapsed");
                btn.innerHTML = '<i class="bi bi-chevron-left"></i>';
            });
        }

        localStorage.setItem("propama_sidebar_collapsed", collapsed);
    }




    // Manejo de categorías
    const categoryLinks = sidebar.querySelectorAll('[data-bs-toggle="collapse"]');
    categoryLinks.forEach(link => {
        const arrow = link.querySelector('.bi-caret-right-fill, .bi-caret-down-fill');
        const collapseId = link.getAttribute('href');
        const collapseEl = document.querySelector(collapseId);

        if (!collapseEl) return;

        // Actualizar flecha
        collapseEl.addEventListener('show.bs.collapse', () => {
            if (arrow) {
                arrow.classList.remove('bi-caret-right-fill');
                arrow.classList.add('bi-caret-down-fill');
            }
        });
        collapseEl.addEventListener('hide.bs.collapse', () => {
            if (arrow) {
                arrow.classList.remove('bi-caret-down-fill');
                arrow.classList.add('bi-caret-right-fill');
            }
        });

        link.addEventListener('click', function (e) {
            if (isCollapsed) {
                e.preventDefault();
                isCollapsed = false;
                applySidebarState(false);
                setTimeout(() => {
                    const bsCollapse = bootstrap.Collapse.getOrCreateInstance(collapseEl);
                    bsCollapse.show();
                }, 310);
            }
        });
    });
})();



