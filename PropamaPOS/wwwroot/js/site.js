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


//document.addEventListener('DOMContentLoaded', function () {
//    const loginForm = document.getElementById('loginForm');

//    if (loginForm) {
//        loginForm.addEventListener('submit', function () {
//            const btnLogin = document.getElementById('btnLogin');
//            const loginText = document.getElementById('loginText');
//            const loginSpinner = document.getElementById('loginSpinner');

//            loginText.classList.add('d-none');
//            loginSpinner.classList.remove('d-none');
//            btnLogin.disabled = true;
//        });
//    }
//});

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

// ===== MEJORAS ESPECÍFICAS PARA VISTAS DE ACCOUNT =====

// Inicialización de componentes de Account
document.addEventListener("DOMContentLoaded", function () {
    initializeAccountComponents();
});

function initializeAccountComponents() {
    // Toggle password visibility para Account
    document.querySelectorAll(".toggle-password").forEach(btn => {
        btn.addEventListener("click", function () {
            const inputGroup = this.closest('.input-group');
            const input = inputGroup.querySelector('input');
            const icon = this.querySelector("i");

            if (input.type === "password") {
                input.type = "text";
                icon.classList.remove("bi-eye");
                icon.classList.add("bi-eye-slash");
                this.classList.add("active");
            } else {
                input.type = "password";
                icon.classList.remove("bi-eye-slash");
                icon.classList.add("bi-eye");
                this.classList.remove("active");
            }
        });
    });

    // Validación de formularios de Account - VERSIÓN SIMPLIFICADA
    const accountForms = document.querySelectorAll('.account-form');
    accountForms.forEach(form => {
        form.addEventListener('submit', function (e) {
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn && !submitBtn.disabled) {
                // Solo deshabilitar el botón, el spinner ya se maneja en la vista
                submitBtn.disabled = true;
                submitBtn.classList.add('btn-loading');
            }
        });
    });

    // Efectos de focus para inputs de Account
    const accountInputs = document.querySelectorAll('.account-form .form-control');
    accountInputs.forEach(input => {
        input.addEventListener('focus', function () {
            this.parentElement.classList.add('focused');
        });

        input.addEventListener('blur', function () {
            if (!this.value) {
                this.parentElement.classList.remove('focused');
            }
        });
    });

    // Animación de entrada para el login container
    const loginContainer = document.querySelector('.login-container');
    if (loginContainer) {
        setTimeout(() => {
            loginContainer.style.opacity = '0';
            loginContainer.style.transform = 'translateY(20px)';
            loginContainer.style.transition = 'all 0.5s ease';

            setTimeout(() => {
                loginContainer.style.opacity = '1';
                loginContainer.style.transform = 'translateY(0)';
            }, 50);
        }, 100);
    }
}

// Función para mostrar/ocultar loading states
function setButtonLoading(button, isLoading) {
    if (isLoading) {
        button.disabled = true;
        button.classList.add('btn-loading');
    } else {
        button.disabled = false;
        button.classList.remove('btn-loading');
    }
}



// ===== FUNCIONALIDADES ESPECÍFICAS PARA ADMIN =====

// Inicialización de tooltips
function initializeAdminTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    const tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Validación de formularios mejorada
function initializeFormValidation() {
    // Bootstrap validation
    'use strict';
    window.addEventListener('load', function () {
        const forms = document.getElementsByClassName('needs-validation');
        Array.prototype.filter.call(forms, function (form) {
            form.addEventListener('submit', function (event) {
                if (form.checkValidity() === false) {
                    event.preventDefault();
                    event.stopPropagation();

                    // Scroll al primer error
                    const firstInvalid = form.querySelector('.is-invalid');
                    if (firstInvalid) {
                        firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }
                }
                form.classList.add('was-validated');
            }, false);
        });
    }, false);
}

// Mejoras para tablas responsivas
function enhanceAdminTables() {
    const tables = document.querySelectorAll('.table-admin');

    tables.forEach(table => {
        // Añadir clases para mejorar la experiencia en móvil
        if (window.innerWidth < 768) {
            table.classList.add('table-sm');
        }
    });
}

// Funcionalidad de filtros avanzados
function initializeAdminFilters() {
    const filterToggles = document.querySelectorAll('.filter-toggle');

    filterToggles.forEach(toggle => {
        toggle.addEventListener('click', function () {
            const target = document.querySelector(this.getAttribute('data-bs-target'));
            if (target) {
                target.classList.toggle('show');
            }
        });
    });
}

// Contador de caracteres para campos de texto
function initializeCharacterCounters() {
    const counters = document.querySelectorAll('[data-max-length]');

    counters.forEach(counter => {
        const maxLength = parseInt(counter.getAttribute('data-max-length'));
        const input = counter.querySelector('input, textarea');
        const countDisplay = counter.querySelector('.char-count');

        if (input && countDisplay) {
            input.addEventListener('input', function () {
                const currentLength = this.value.length;
                countDisplay.textContent = `${currentLength}/${maxLength}`;

                if (currentLength > maxLength * 0.8) {
                    countDisplay.classList.add('text-warning');
                } else {
                    countDisplay.classList.remove('text-warning');
                }

                if (currentLength >= maxLength) {
                    countDisplay.classList.add('text-danger');
                } else {
                    countDisplay.classList.remove('text-danger');
                }
            });

            // Trigger inicial
            input.dispatchEvent(new Event('input'));
        }
    });
}

// Mejoras para selects con búsqueda
function initializeEnhancedSelects() {
    const enhancedSelects = document.querySelectorAll('.enhanced-select');

    enhancedSelects.forEach(select => {
        // Añadir funcionalidad de búsqueda si es necesario
        if (select.hasAttribute('data-search')) {
            const searchInput = document.createElement('input');
            searchInput.type = 'text';
            searchInput.className = 'form-control mb-2';
            searchInput.placeholder = 'Buscar...';

            select.parentNode.insertBefore(searchInput, select);

            searchInput.addEventListener('input', function () {
                const searchTerm = this.value.toLowerCase();
                const options = select.querySelectorAll('option');

                options.forEach(option => {
                    if (option.textContent.toLowerCase().includes(searchTerm)) {
                        option.style.display = '';
                    } else {
                        option.style.display = 'none';
                    }
                });
            });
        }
    });
}

// Animaciones de carga mejoradas
function showLoadingState(button) {
    const originalText = button.innerHTML;
    button.innerHTML = `
        <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
        Procesando...
    `;
    button.disabled = true;

    return function () {
        button.innerHTML = originalText;
        button.disabled = false;
    };
}

// Confirmaciones mejoradas para acciones destructivas
function initializeEnhancedConfirmations() {
    const destructiveButtons = document.querySelectorAll('[data-destructive]');

    destructiveButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            const message = this.getAttribute('data-confirm-message') ||
                '¿Está seguro de que desea realizar esta acción?';

            if (!confirm(message)) {
                e.preventDefault();
                e.stopPropagation();
                return false;
            }
        });
    });
}

// Auto-hide para alertas
function initializeAutoHideAlerts() {
    const autoHideAlerts = document.querySelectorAll('.alert[data-auto-hide]');

    autoHideAlerts.forEach(alert => {
        const delay = parseInt(alert.getAttribute('data-auto-hide')) || 5000;

        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, delay);
    });
}

// Mejoras para la paginación
function initializePaginationEnhancements() {
    const paginationLinks = document.querySelectorAll('.pagination-admin .page-link');

    paginationLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            if (!this.getAttribute('href')) {
                e.preventDefault();
                return;
            }

            // Añadir efecto de loading si es necesario
            const targetRow = this.closest('tr');
            if (targetRow) {
                targetRow.classList.add('table-active');
            }
        });
    });
}

// Inicialización cuando el DOM está listo
document.addEventListener('DOMContentLoaded', function () {
    initializeAdminTooltips();
    initializeFormValidation();
    enhanceAdminTables();
    initializeAdminFilters();
    initializeCharacterCounters();
    initializeEnhancedSelects();
    initializeEnhancedConfirmations();
    initializeAutoHideAlerts();
    initializePaginationEnhancements();

    // Añadir clase fade-in a los elementos principales
    const mainContent = document.querySelector('.fade-in');
    if (mainContent) {
        mainContent.style.opacity = '0';
        mainContent.style.transform = 'translateY(20px)';

        setTimeout(() => {
            mainContent.style.transition = 'all 0.5s ease';
            mainContent.style.opacity = '1';
            mainContent.style.transform = 'translateY(0)';
        }, 100);
    }
});

// Mejoras para responsive
window.addEventListener('resize', function () {
    enhanceAdminTables();
});

// Exportar funciones para uso global
window.AdminUtils = {
    showLoadingState,
    initializeAdminTooltips,
    initializeFormValidation
};



// ===== FUNCIONALIDADES ESPECÍFICAS PARA AJUSTES =====

// Inicialización de tooltips
function initializeTooltips() {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    if (tooltipTriggerList.length > 0 && typeof bootstrap !== 'undefined') {
        [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));
    }
}

// Validación de formularios mejorada
function initializeFormValidation() {
    const forms = document.querySelectorAll('.needs-validation');

    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();

                // Mostrar todos los mensajes de error
                const invalidFields = form.querySelectorAll(':invalid');
                invalidFields.forEach(field => {
                    field.classList.add('is-invalid');
                });

                // Scroll al primer campo inválido
                const firstInvalid = form.querySelector(':invalid');
                if (firstInvalid) {
                    firstInvalid.scrollIntoView({
                        behavior: 'smooth',
                        block: 'center'
                    });
                }
            }

            form.classList.add('was-validated');
        }, false);
    });
}

// Manejo de alertas dinámicas
function showAlert(type, message, container = null) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show mt-3`;
    alertDiv.innerHTML = `
        <i class="bi ${getAlertIcon(type)} me-2"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    const targetContainer = container || document.querySelector('.container-fluid') || document.body;
    targetContainer.prepend(alertDiv);

    // Auto-remover después de 5 segundos
    setTimeout(() => {
        if (alertDiv.parentNode) {
            const bsAlert = new bootstrap.Alert(alertDiv);
            bsAlert.close();
        }
    }, 5000);
}

function getAlertIcon(type) {
    const icons = {
        'success': 'bi-check-circle',
        'warning': 'bi-exclamation-triangle',
        'danger': 'bi-x-circle',
        'info': 'bi-info-circle'
    };
    return icons[type] || 'bi-info-circle';
}

// Manejo de estados vacíos en tablas
function toggleEmptyState(tableId, emptyStateId) {
    const table = document.getElementById(tableId);
    const emptyState = document.getElementById(emptyStateId);

    if (table && emptyState) {
        const hasRows = table.querySelector('tbody tr') !== null;
        if (hasRows) {
            emptyState.classList.add('d-none');
        } else {
            emptyState.classList.remove('d-none');
        }
    }
}

// Inicialización cuando el DOM está listo
document.addEventListener('DOMContentLoaded', function () {
    initializeTooltips();
    initializeFormValidation();

    // Inicializar estados vacíos
    const tables = document.querySelectorAll('table[data-empty-state]');
    tables.forEach(table => {
        const emptyStateId = table.getAttribute('data-empty-state');
        toggleEmptyState(table.id, emptyStateId);
    });
});

// Exportar funciones para uso global
window.PropamaPOS = {
    ...window.PropamaPOS,
    showAlert,
    initializeTooltips,
    initializeFormValidation,
    toggleEmptyState
};








// ===== FUNCIONALIDADES ESPECÍFICAS PARA CATEGORÍAS =====

// Inicialización de componentes de categoría
function initializeCategoryComponents() {
    // Tooltips para acciones
    const categoryTooltips = document.querySelectorAll('.btn-category-action[data-bs-toggle="tooltip"]');
    if (categoryTooltips.length > 0 && typeof bootstrap !== 'undefined') {
        [...categoryTooltips].map(el => new bootstrap.Tooltip(el));
    }

    // Confirmación mejorada para eliminaciones
    const deleteButtons = document.querySelectorAll('.btn-category-delete');
    deleteButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            const categoryName = this.closest('tr').querySelector('.fw-semibold').textContent;
            if (!confirm(`¿Estás seguro de que quieres eliminar la categoría "${categoryName.trim()}"? Esta acción no se puede deshacer.`)) {
                e.preventDefault();
            }
        });
    });

    // Mejoras para formularios de categoría
    const categoryForms = document.querySelectorAll('.category-form');
    categoryForms.forEach(form => {
        // Validación en tiempo real
        const inputs = form.querySelectorAll('input, textarea');
        inputs.forEach(input => {
            input.addEventListener('blur', function () {
                validateCategoryField(this);
            });

            input.addEventListener('input', function () {
                clearFieldValidation(this);
            });
        });

        // Submit con loading state
        form.addEventListener('submit', function (e) {
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn && this.checkValidity()) {
                setButtonLoading(submitBtn, true);
            }
        });
    });
}

// Validación de campos de categoría
function validateCategoryField(field) {
    const value = field.value.trim();
    const formGroup = field.closest('.mb-3');

    // Limpiar estados previos
    clearFieldValidation(field);

    // Validar campo requerido
    if (field.hasAttribute('required') && !value) {
        showFieldError(field, 'Este campo es requerido');
        return false;
    }

    // Validar longitud mínima para nombre
    if (field.name === 'Nombre' && value.length < 2) {
        showFieldError(field, 'El nombre debe tener al menos 2 caracteres');
        return false;
    }

    return true;
}

// Mostrar error en campo
function showFieldError(field, message) {
    const formGroup = field.closest('.mb-3');
    field.classList.add('is-invalid');

    let errorElement = formGroup.querySelector('.field-error');
    if (!errorElement) {
        errorElement = document.createElement('div');
        errorElement.className = 'field-error text-danger small mt-1';
        formGroup.appendChild(errorElement);
    }
    errorElement.textContent = message;
}

// Limpiar validación de campo
function clearFieldValidation(field) {
    field.classList.remove('is-invalid');
    field.classList.remove('is-valid');

    const formGroup = field.closest('.mb-3');
    const errorElement = formGroup.querySelector('.field-error');
    if (errorElement) {
        errorElement.remove();
    }
}

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function () {
    initializeCategoryComponents();
});

// Exportar funciones para uso global
window.CategoryUtils = {
    initializeCategoryComponents,
    validateCategoryField
};




// Agregar al final del FILE: PropamaPOS/wwwroot/js/site.js

// ===== FUNCIONALIDADES ESPECÍFICAS PARA DASHBOARD EMPLEADO =====

function initializeDashboardComponents() {
    // Animación de entrada para las cards
    const dashboardCards = document.querySelectorAll('.dashboard-card');
    dashboardCards.forEach((card, index) => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';

        setTimeout(() => {
            card.style.transition = 'all 0.5s ease';
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, index * 100);
    });

    // Tooltips para elementos del dashboard
    const dashboardTooltips = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    if (dashboardTooltips.length > 0 && typeof bootstrap !== 'undefined') {
        [...dashboardTooltips].map(el => new bootstrap.Tooltip(el));
    }

    // Actualización automática del reloj
    updateLiveClock();

    // Efectos hover mejorados para botones de acción rápida
    const quickActionButtons = document.querySelectorAll('.btn[asp-controller]');
    quickActionButtons.forEach(btn => {
        btn.addEventListener('mouseenter', function () {
            this.style.transform = 'translateY(-2px)';
        });

        btn.addEventListener('mouseleave', function () {
            this.style.transform = 'translateY(0)';
        });
    });
}

function updateLiveClock() {
    const clockElement = document.getElementById('liveClock');
    if (clockElement) {
        setInterval(() => {
            const now = new Date();
            clockElement.textContent = now.toLocaleTimeString('es-GT', {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
        }, 1000);
    }
}

// Inicializar componentes del dashboard cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function () {
    initializeDashboardComponents();
});

// Exportar funciones para uso global
window.DashboardUtils = {
    initializeDashboardComponents,
    updateLiveClock
};