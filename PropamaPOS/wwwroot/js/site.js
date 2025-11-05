// Sidebar toggle mejorado
document.addEventListener("DOMContentLoaded", function () {
    const sidebar = document.getElementById("appSidebar");
    const toggleBtn = document.getElementById("sidebarToggleBtn");
    const body = document.body;

    // 🔹 Si no hay sidebar, quitar margen y salir
    if (!sidebar) {
        body.classList.remove("sidebar-hidden");
        body.classList.remove("preload");
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
        body.classList.remove("preload");
    }, 100);

    toggleBtn.addEventListener("click", function () {
        hidden = !hidden;
        applySidebarState(hidden);
    });

    // Cerrar sidebar en móvil al hacer clic fuera - SIN OVERLAY
    document.addEventListener('click', function (e) {
        if (window.innerWidth <= 767.98 &&
            !sidebar.classList.contains('hidden') &&
            !sidebar.contains(e.target) &&
            e.target !== toggleBtn &&
            !toggleBtn.contains(e.target) &&
            !e.target.closest('.navbar-toggler')) {
            hidden = true;
            applySidebarState(hidden);
        }
    });

    // Manejar redimensionamiento
    window.addEventListener('resize', function () {
        if (window.innerWidth > 767.98) {
            // En desktop, restaurar estado guardado
            const savedState = localStorage.getItem("propama_sidebar_hidden") === "true";
            applySidebarState(savedState, true);
        }
    });

    function applySidebarState(isHidden, instant = false) {
        const transition = instant ? "none" : "transform 0.35s cubic-bezier(0.4, 0, 0.2, 1)";
        sidebar.style.transition = transition;

        if (isHidden) {
            body.classList.add("sidebar-hidden");
            sidebar.classList.add("hidden");
        } else {
            body.classList.remove("sidebar-hidden");
            sidebar.classList.remove("hidden");
        }

        localStorage.setItem("propama_sidebar_hidden", isHidden);

        // En móvil, NO agregar overlay (comentado)
        // if (window.innerWidth <= 767.98 && !isHidden) {
        //     addMobileOverlay();
        // } else {
        //     removeMobileOverlay();
        // }
    }

    // Eliminar las funciones de overlay o mantenerlas comentadas
    /*
    function addMobileOverlay() {
        // Comentado - no usar overlay
    }

    function removeMobileOverlay() {
        // Comentado - no usar overlay
    }
    */
});


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

// Mejora para tablas responsivas
document.addEventListener("DOMContentLoaded", function () {
    // Agregar clases responsivas a tablas
    document.querySelectorAll('table').forEach(table => {
        if (!table.closest('.table-responsive')) {
            table.classList.add('table', 'table-hover');
            const wrapper = document.createElement('div');
            wrapper.className = 'table-responsive';
            table.parentNode.insertBefore(wrapper, table);
            wrapper.appendChild(table);
        }
    });
});