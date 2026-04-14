//C# записва нотификациите като JSON string в TempData
//Razor View чете JSON-а и го преобразува в HTML (div.taost-notification)
// При зареждане на страницата JavaScript намира тези елементи и ги показва
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

// При AJAX заявка сървърът връща JSON (message + cssClass)
// JavaScript го получава и извиква showToast, за да създаде нотификация динамично
function showToast(message, cssClass) {
    const container = document.getElementById("toast-container");
    if (!container) {
        return;
    }

    const toast = document.createElement("div");
    toast.className = `toast-notification ${cssClass || "toast-info"}`;
    toast.textContent = message;
    container.appendChild(toast);

    const currentToasts = container.querySelectorAll(".toast-notification");
    const index = currentToasts.length - 1;

    setTimeout(() => {
        toast.style.opacity = "0";
        toast.style.transform = "translateY(-10px)";
        toast.style.transition = "0.3s ease";

        setTimeout(() => toast.remove(), 300);
    }, 3000 + index * 300);
}

window.showToast = showToast;
