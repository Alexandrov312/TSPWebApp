//C# записва нотификациите като JSON string в TempData
//Razor View чете JSON-а и го преобразува в HTML (div.toast-notification)
//При зареждане на страницата JavaScript намира тези елементи и ги показва

//DOM е дървовидна структура на HTML документа, която позволява на JavaScript да достъпва и променя елементите на страницата.
document.addEventListener("DOMContentLoaded", function () {
    const toasts = document.querySelectorAll(".toast-notification");

    toasts.forEach((toast, index) => {
        //При повече от една нотификация ги скрива с леко забавяне една след друга
        setTimeout(() => {
            toast.style.opacity = "0";
            toast.style.transform = "translateY(-10px)";
            toast.style.transition = "0.3s ease";

            setTimeout(() => toast.remove(), 300);
        }, 3000 + index * 300);
    });
});

//При AJAX заявка сървърът връща JSON (message + cssClass)
//JavaScript го получава и извиква showToast, за да създаде нотификация динамично
function showToast(message, cssClass) {
    const container = document.getElementById("toast-container");
    if (!container) {
        return;
    }

    const toast = document.createElement("div");
    toast.className = `toast-notification ${cssClass || "toast-info"}`;
    toast.textContent = message;
    container.appendChild(toast);

    //Новите toast-и чакат малко повече, за да не изчезват всички наведнъж
    const currentToasts = container.querySelectorAll(".toast-notification");
    const index = currentToasts.length - 1;

    setTimeout(() => {
        toast.style.opacity = "0";
        toast.style.transform = "translateY(-10px)";
        toast.style.transition = "0.3s ease";

        setTimeout(() => toast.remove(), 300);
    }, 3000 + index * 300);
}

//Нормализира текст от contenteditable елемент
function normalizeEditableValue(element) {
    // \u00A0 - non-breaking space (NBSP)
    // „Невидим“ специален интервал, който не позволява пренасяне на ред и трябва да се нормализира, 
    // защото се държи различно от нормален space.
    // "Hello World" !== "Hello\u00A0World"
    return element?.textContent?.replace(/\u00A0/g, " ").trim() ?? "";
}

window.normalizeEditableValue = normalizeEditableValue;
window.showToast = showToast;
