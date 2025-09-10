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

    // Toggle para campos de contraseña
    const passwordToggle = document.getElementById('cambiarPassword');
    if (passwordToggle) {
        passwordToggle.addEventListener('change', function () {
            const passwordFields = document.getElementById('passwordFields');
            const passwordInputs = passwordFields.querySelectorAll('input[type="password"]');

            if (this.checked) {
                passwordFields.style.display = 'block';
                passwordInputs.forEach(input => input.setAttribute('required', 'required'));
            } else {
                passwordFields.style.display = 'none';
                passwordInputs.forEach(input => {
                    input.removeAttribute('required');
                    input.value = '';
                });
            }
        });
    }
});