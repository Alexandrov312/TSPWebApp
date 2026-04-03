// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {
    const toasts = document.querySelectorAll(".toast-notification");

    toasts.forEach((toast, index) => {
        setTimeout(() => {
            toast.style.opacity = "0";
            toast.style.transform = "translateY(-10px)";
            toast.style.transition = "0.3s ease";

            setTimeout(() => toast.remove(), 300);
        }, 3000 + index * 300);
    });
});