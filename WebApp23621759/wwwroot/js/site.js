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

    applyPersistedViewModes();
    applyPersistedTheme();
});

document.addEventListener("click", function (event) {
    const isPopupRelatedClick = event.target.closest(".show-popup, .show-done-popup, .delete-popup, .done-popup, .delete-wrapper, .done-wrapper");
    if (!isPopupRelatedClick && typeof hideAllTaskPopups === "function") {
        hideAllTaskPopups();
    }
}, true);

document.addEventListener("click", function (event) {
    const themeToggle = event.target.closest("[data-theme-toggle]");
    if (themeToggle) {
        toggleTheme();
        return;
    }

    const toggle = event.target.closest("[data-view-toggle]");
    if (!toggle) {
        return;
    }

    const viewName = toggle.dataset.viewToggle;
    const activeMode = toggle.querySelector("[data-view-mode-button].active")?.dataset.viewModeButton ?? "table";
    const mode = activeMode === "table" ? "grid" : "table";
    if (!viewName) {
        return;
    }

    setViewMode(viewName, mode);
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
    //\u00A0 е non-breaking space (NBSP)
    //Това е невидим специален интервал, който не позволява пренасяне на ред
    //Нормализира се, защото се държи различно от нормален space
    //"Hello World" !== "Hello\u00A0World"
    return element?.textContent?.replace(/\u00A0/g, " ").trim() ?? "";
}

window.normalizeEditableValue = normalizeEditableValue;
window.showToast = showToast;

//Прилага light/dark тема и я пази за следващо зареждане
function setTheme(theme) {
    const normalizedTheme = theme === "dark" ? "dark" : "light";
    document.documentElement.dataset.theme = normalizedTheme;
    localStorage.setItem("site:theme", normalizedTheme);

    document.querySelectorAll("[data-theme-toggle]").forEach(button => {
        const isDark = normalizedTheme === "dark";
        button.setAttribute("aria-pressed", String(isDark));
        button.querySelector("[data-theme-icon]").textContent = isDark ? "☀️" : "🌙";
        button.querySelector("[data-theme-text]").textContent = isDark ? "Light" : "Dark";
    });
}

//Сменя текущата тема към другата палитра
function toggleTheme() {
    const currentTheme = document.documentElement.dataset.theme === "dark" ? "dark" : "light";
    setTheme(currentTheme === "dark" ? "light" : "dark");
}

//Възстановява избраната тема след refresh
function applyPersistedTheme() {
    setTheme(localStorage.getItem("site:theme") || document.documentElement.dataset.theme || "light");
}

//Прилага избрания table/grid режим и го пази за следващо отваряне на страницата
function setViewMode(viewName, mode) {
    if (viewName === "mytasks") {
        document.documentElement.dataset.mytasksView = mode;
    }

    if (viewName === "archive") {
        document.documentElement.dataset.archiveView = mode;
    }

    document.querySelectorAll(`[data-view-toggle="${viewName}"] [data-view-mode-button]`)
        .forEach(button => button.classList.toggle("active", button.dataset.viewModeButton === mode));

    document.querySelectorAll(`[data-view-mode-panel]`)
        .forEach(panel => {
            const isMatchingPanel = panel.closest(`.${viewName}-page, .mytasks-list-view, .archive-card`);
            if (isMatchingPanel) {
                panel.classList.toggle("is-hidden", panel.dataset.viewModePanel !== mode);
            }
        });

    localStorage.setItem(`${viewName}:viewMode`, mode);
}

//Възстановява table/grid режима след refresh или AJAX презареждане на toolbar-а
function applyPersistedViewModes() {
    document.querySelectorAll("[data-view-toggle]").forEach(toggle => {
        const viewName = toggle.dataset.viewToggle;
        const savedMode = localStorage.getItem(`${viewName}:viewMode`) || "table";
        setViewMode(viewName, savedMode);
    });
}

window.setViewMode = setViewMode;
window.applyPersistedViewModes = applyPersistedViewModes;
window.setTheme = setTheme;
window.toggleTheme = toggleTheme;
