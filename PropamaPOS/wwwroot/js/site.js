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

    // aplicar estado guardado
    const collapsed = localStorage.getItem('propama_sidebar_collapsed') === 'true';
    if (collapsed) {
        sidebar.classList.add('collapsed');
        document.body.classList.add('sidebar-collapsed');
        btn.innerHTML = '<i class="bi bi-chevron-right"></i>';
    }

    btn.addEventListener('click', function (e) {
        e.preventDefault();
        const isCollapsed = sidebar.classList.toggle('collapsed');
        if (isCollapsed) {
            document.body.classList.add('sidebar-collapsed');
            btn.innerHTML = '<i class="bi bi-chevron-right"></i>';
        } else {
            document.body.classList.remove('sidebar-collapsed');
            btn.innerHTML = '<i class="bi bi-chevron-left"></i>';
        }
        localStorage.setItem('propama_sidebar_collapsed', isCollapsed);
    });

    // Cerrar submenús al colapsar
    if (isCollapsed) {
        const openMenus = sidebar.querySelectorAll('.collapse.show');
        openMenus.forEach(menu => {
            const bsCollapse = bootstrap.Collapse.getOrCreateInstance(menu);
            bsCollapse.hide();
        });
    }


})();

