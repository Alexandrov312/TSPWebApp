//C# записва нотификациите като JSON string в TempData.
//Razor View чете JSON-а и го преобразува в HTML.
//При зареждане на страницата JavaScript намира тези елементи и ги управлява.

document.addEventListener("DOMContentLoaded", function () {
    initializeToasts();
    applyPersistedViewModes();
    applyPersistedTheme();
    initializeNotificationBell();
    initializePasswordPeek();
});

//Глобален listener, който затваря task popup-и при клик извън тях.
document.addEventListener("click", function (event) {
    const isPopupRelatedClick = event.target.closest(".show-popup, .show-done-popup, .delete-popup, .done-popup, .delete-wrapper, .done-wrapper");
    if (!isPopupRelatedClick && typeof hideAllTaskPopups === "function") {
        hideAllTaskPopups();
    }
}, true);

//Глобален listener за theme toggle, toast close, view toggle и notification bell.
document.addEventListener("click", function (event) {
    const themeToggle = event.target.closest("[data-theme-toggle]");
    if (themeToggle) {
        toggleTheme();
        return;
    }

    const toastCloseButton = event.target.closest("[data-toast-close]");
    if (toastCloseButton) {
        dismissToast(toastCloseButton.closest(".toast-notification"));
        return;
    }

    const toggle = event.target.closest("[data-view-toggle]");
    if (toggle) {
        const viewName = toggle.dataset.viewToggle;
        const activeMode = toggle.querySelector("[data-view-mode-button].active")?.dataset.viewModeButton ?? "table";
        const mode = activeMode === "table" ? "grid" : "table";
        if (viewName) {
            setViewMode(viewName, mode);
        }
        return;
    }

    const notificationToggle = event.target.closest("[data-notification-toggle]");
    const notificationBell = document.querySelector("[data-notification-bell]");
    if (notificationToggle && notificationBell) {
        toggleNotificationBell(notificationBell);
        return;
    }

    const dismissNotificationButton = event.target.closest("[data-notification-dismiss]");
    if (dismissNotificationButton) {
        const notificationItem = dismissNotificationButton.closest("[data-notification-item]");
        dismissNotification(notificationItem);
        return;
    }

    const notificationItem = event.target.closest("[data-notification-item]");
    if (notificationItem && !event.target.closest("[data-notification-dismiss]")) {
        markNotificationAsRead(notificationItem);
        return;
    }

    const readAllButton = event.target.closest("[data-notification-read-all]");
    if (readAllButton) {
        markAllNotificationsAsRead();
        return;
    }

    if (notificationBell && !event.target.closest("[data-notification-bell]") && !event.target.closest(".notification-bell-panel")) {
        closeNotificationBell(notificationBell);
    }
});

//Password peek работи с press-and-hold, затова слуша pointer събития вместо обикновен click.
document.addEventListener("pointerdown", handlePasswordPeekStart);
document.addEventListener("pointerup", handlePasswordPeekEnd);
document.addEventListener("pointercancel", handlePasswordPeekEnd);
document.addEventListener("mouseleave", handlePasswordPeekEnd);

//При scroll/resize позицията на изнесения notification panel се синхронизира спрямо камбанката.
window.addEventListener("resize", syncOpenNotificationBellPosition);
window.addEventListener("scroll", syncOpenNotificationBellPosition, true);

//При AJAX заявка сървърът връща JSON (message + cssClass).
//JavaScript го получава и извиква showToast, за да създаде нотификация динамично.
function showToast(message, cssClass) {
    const container = document.getElementById("toast-container");
    if (!container) {
        return;
    }

    const toast = document.createElement("div");
    toast.className = `toast-notification ${cssClass || "toast-info"}`;
    toast.innerHTML = `
        <span class="toast-notification-text"></span>
        <button type="button" class="toast-close-btn" data-toast-close aria-label="Close notification">\u00D7</button>
    `;

    toast.querySelector(".toast-notification-text").textContent = message;
    container.appendChild(toast);

    queueToastDismiss(toast, 3000 + (container.querySelectorAll(".toast-notification").length - 1) * 300);
}

//Нормализира текст от contenteditable елемент.
function normalizeEditableValue(element) {
    return element?.textContent?.replace(/\u00A0/g, " ").trim() ?? "";
}

window.normalizeEditableValue = normalizeEditableValue;
window.showToast = showToast;

//Прилага light/dark тема и я пази за следващо зареждане.
function setTheme(theme) {
    const normalizedTheme = theme === "dark" ? "dark" : "light";
    document.documentElement.dataset.theme = normalizedTheme;
    localStorage.setItem("site:theme", normalizedTheme);

    document.querySelectorAll("[data-theme-toggle]").forEach(button => {
        const isDark = normalizedTheme === "dark";
        const icon = button.querySelector("[data-theme-icon]");
        const text = button.querySelector("[data-theme-text]");

        button.setAttribute("aria-pressed", String(isDark));
        if (icon) {
            icon.textContent = isDark ? "\u2600\uFE0F" : "\uD83C\uDF19";
        }
        if (text) {
            text.textContent = isDark ? "Light" : "Dark";
        }
    });
}

//Сменя текущата тема към другата палитра.
function toggleTheme() {
    const currentTheme = document.documentElement.dataset.theme === "dark" ? "dark" : "light";
    setTheme(currentTheme === "dark" ? "light" : "dark");
}

//Подготвя всички password полета с начален state за стабилна press-and-hold визуализация.
function initializePasswordPeek() {
    document.querySelectorAll("[data-password-peek]").forEach(wrapper => {
        const input = wrapper.querySelector('input[type="password"], input[type="text"]');
        const toggleButton = wrapper.querySelector("[data-password-peek-toggle]");
        if (input) {
            input.dataset.passwordPeekInput = "true";
        }
        if (toggleButton) {
            const isVisible = input?.type === "text";
            toggleButton.dataset.peekState = isVisible ? "visible" : "hidden";
            toggleButton.setAttribute("aria-pressed", isVisible ? "true" : "false");
        }
    });
}

//Показва паролата само докато бутонът е физически задържан.
function handlePasswordPeekStart(event) {
    const toggleButton = event.target.closest("[data-password-peek-toggle]");
    if (!toggleButton) {
        return;
    }

    event.preventDefault();
    const wrapper = toggleButton.closest("[data-password-peek]");
    const input = wrapper?.querySelector("[data-password-peek-input]");
    if (!(input instanceof HTMLInputElement)) {
        return;
    }

    input.type = "text";
    toggleButton.dataset.peekState = "visible";
    toggleButton.setAttribute("aria-pressed", "true");
    toggleButton.dataset.peekActive = "true";
}

//Скрива паролата веднага щом бутонът бъде отпуснат.
function handlePasswordPeekEnd() {
    document.querySelectorAll('[data-password-peek-toggle][data-peek-active="true"]').forEach(toggleButton => {
        const wrapper = toggleButton.closest("[data-password-peek]");
        const input = wrapper?.querySelector("[data-password-peek-input]");
        if (input instanceof HTMLInputElement) {
            input.type = "password";
        }

        toggleButton.dataset.peekState = "hidden";
        toggleButton.setAttribute("aria-pressed", "false");
        delete toggleButton.dataset.peekActive;
    });
}

//Възстановява избраната тема след refresh.
function applyPersistedTheme() {
    setTheme(localStorage.getItem("site:theme") || document.documentElement.dataset.theme || "light");
}

//Прилага избрания table/grid режим и го пази за следващо отваряне на страницата.
function setViewMode(viewName, mode) {
    if (viewName === "mytasks") {
        document.documentElement.dataset.mytasksView = mode;
    }

    if (viewName === "archive") {
        document.documentElement.dataset.archiveView = mode;
    }

    document.querySelectorAll(`[data-view-toggle="${viewName}"] [data-view-mode-button]`)
        .forEach(button => button.classList.toggle("active", button.dataset.viewModeButton === mode));

    document.querySelectorAll("[data-view-mode-panel]")
        .forEach(panel => {
            const isMatchingPanel = panel.closest(`.${viewName}-page, .mytasks-list-view, .archive-card`);
            if (isMatchingPanel) {
                panel.classList.toggle("is-hidden", panel.dataset.viewModePanel !== mode);
            }
        });

    localStorage.setItem(`${viewName}:viewMode`, mode);
}

//Възстановява table/grid режима след refresh или AJAX презареждане на toolbar-а.
function applyPersistedViewModes() {
    document.querySelectorAll("[data-view-toggle]").forEach(toggle => {
        const viewName = toggle.dataset.viewToggle;
        const savedMode = localStorage.getItem(`${viewName}:viewMode`) || "table";
        setViewMode(viewName, savedMode);
    });
}

//Подготвя toast нотификациите след зареждане.
function initializeToasts() {
    const toasts = document.querySelectorAll(".toast-notification");
    toasts.forEach((toast, index) => {
        if (!toast.querySelector("[data-toast-close]")) {
            const closeButton = document.createElement("button");
            closeButton.type = "button";
            closeButton.className = "toast-close-btn";
            closeButton.setAttribute("data-toast-close", "");
            closeButton.setAttribute("aria-label", "Close notification");
            closeButton.textContent = "\u00D7";
            toast.appendChild(closeButton);
        }

        queueToastDismiss(toast, 3000 + index * 300);
    });
}

//Планира автоматичното скриване на toast нотификацията.
function queueToastDismiss(toast, delay) {
    if (!toast) {
        return;
    }

    if (toast.dataset.dismissScheduled === "true") {
        return;
    }

    toast.dataset.dismissScheduled = "true";
    window.setTimeout(() => dismissToast(toast), delay);
}

//Затваря toast нотификацията плавно.
function dismissToast(toast) {
    if (!toast || toast.dataset.isClosing === "true") {
        return;
    }

    toast.dataset.isClosing = "true";
    toast.style.opacity = "0";
    toast.style.transform = "translateY(-10px)";
    toast.style.transition = "0.3s ease";

    window.setTimeout(() => toast.remove(), 300);
}

//Подготвя камбанката след зареждане.
function initializeNotificationBell() {
    document.querySelectorAll("[data-notification-bell]").forEach(prepareNotificationBell);

    if (document.querySelector("[data-notification-bell]") && !window.notificationBellRefreshInterval) {
        window.notificationBellRefreshInterval = window.setInterval(refreshNotificationBell, 60000);
    }
}

//Премества панела в body, за да бъде винаги над всички елементи.
function prepareNotificationBell(bell) {
    if (!bell) {
        return;
    }

    const panel = bell.querySelector("[data-notification-panel]");
    if (!panel || panel.dataset.portaled === "true") {
        return;
    }

    document.body.appendChild(panel);
    panel.dataset.portaled = "true";
    panel.dataset.portalOwner = bell.dataset.notificationBellId || createNotificationBellId(bell);
}

//Създава стабилен id за връзка между бутона и изнесения панел.
function createNotificationBellId(bell) {
    const id = `notification-bell-${Math.random().toString(36).slice(2, 10)}`;
    bell.dataset.notificationBellId = id;
    return id;
}

//Отваря/затваря панела и позиционира спрямо бутона.
function toggleNotificationBell(bell) {
    const panel = getNotificationPanelForBell(bell);
    if (!panel) {
        return;
    }

    if (bell.classList.contains("is-open")) {
        closeNotificationBell(bell);
        return;
    }

    document.querySelectorAll("[data-notification-bell].is-open").forEach(closeNotificationBell);
    bell.classList.add("is-open");
    panel.classList.add("is-open");
    syncNotificationBellPosition(bell);
}

//Затваря панела на избраната камбанка.
function closeNotificationBell(bell) {
    if (!bell) {
        return;
    }

    bell.classList.remove("is-open");
    const panel = getNotificationPanelForBell(bell);
    if (panel) {
        panel.classList.remove("is-open");
    }
}

//Позиционира панела точно под бутона независимо от layout-а.
function syncNotificationBellPosition(bell) {
    if (!bell) {
        return;
    }

    const panel = getNotificationPanelForBell(bell);
    const toggle = bell.querySelector("[data-notification-toggle]");
    if (!panel || !toggle) {
        return;
    }

    const rect = toggle.getBoundingClientRect();
    const panelWidth = Math.min(380, window.innerWidth - 24);
    const preferredLeft = rect.right - panelWidth;
    const left = Math.max(12, Math.min(preferredLeft, window.innerWidth - panelWidth - 12));

    panel.style.width = `${panelWidth}px`;
    panel.style.top = `${rect.bottom + 12}px`;
    panel.style.left = `${left}px`;
    panel.style.right = "auto";
}

//Синхронизира позицията на текущо отворения панел.
function syncOpenNotificationBellPosition() {
    const openBell = document.querySelector("[data-notification-bell].is-open");
    if (openBell) {
        syncNotificationBellPosition(openBell);
    }
}

//Намира панела, който принадлежи на конкретната камбанка.
function getNotificationPanelForBell(bell) {
    const panelInside = bell.querySelector("[data-notification-panel]");
    if (panelInside) {
        return panelInside;
    }

    const bellId = bell.dataset.notificationBellId;
    return bellId
        ? document.querySelector(`.notification-bell-panel[data-portal-owner="${bellId}"]`)
        : null;
}

//Маркира избраната нотификация като прочетена.
async function markNotificationAsRead(notificationItem) {
    const notificationId = notificationItem?.dataset.notificationId;
    if (!notificationId) {
        return;
    }

    const response = await fetch("/Notification/MarkAsRead", {
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8"
        },
        body: new URLSearchParams({ id: notificationId })
    });

    if (!response.ok) {
        return;
    }

    const result = await response.json();
    notificationItem.classList.remove("is-unread");
    updateNotificationCount(result.unreadCount);
}

//Затваря нотификацията от панела и я маркира като прочетена.
async function dismissNotification(notificationItem) {
    if (!notificationItem) {
        return;
    }

    await markNotificationAsRead(notificationItem);
    notificationItem.remove();
    ensureNotificationEmptyState();
}

//Маркира всички нотификации като прочетени.
async function markAllNotificationsAsRead() {
    const response = await fetch("/Notification/MarkAllAsRead", {
        method: "POST"
    });

    if (!response.ok) {
        return;
    }

    const result = await response.json();
    document.querySelectorAll("[data-notification-item]").forEach(item => item.remove());
    updateNotificationCount(result.unreadCount);
    ensureNotificationEmptyState();
}

//Синхронизира unread badge-а.
function updateNotificationCount(count) {
    document.querySelectorAll("[data-notification-count]").forEach(countElement => {
        countElement.textContent = String(count);
        countElement.classList.toggle("is-hidden", !count || Number(count) <= 0);
    });
}

//Показва empty state, ако всички нотификации са затворени.
function ensureNotificationEmptyState() {
    document.querySelectorAll(".notification-bell-list").forEach(list => {
        if (list.querySelector("[data-notification-item]")) {
            return;
        }

        if (!list.querySelector(".notification-empty-state")) {
            const emptyState = document.createElement("div");
            emptyState.className = "notification-empty-state";
            emptyState.textContent = "No notifications yet.";
            list.appendChild(emptyState);
        }
    });
}

//Презарежда HTML-а на камбанката без full reload.
async function refreshNotificationBell() {
    const currentBell = document.querySelector("[data-notification-bell]");
    if (!currentBell) {
        return;
    }

    const response = await fetch("/Notification/Bell", { headers: { "X-Requested-With": "XMLHttpRequest" } });
    if (!response.ok) {
        return;
    }

    const html = await response.text();
    const wrapper = document.createElement("div");
    wrapper.innerHTML = html.trim();
    const nextBell = wrapper.firstElementChild;
    if (!nextBell) {
        return;
    }

    const previousPanel = getNotificationPanelForBell(currentBell);
    if (previousPanel) {
        previousPanel.remove();
    }

    currentBell.replaceWith(nextBell);
    prepareNotificationBell(nextBell);
}

window.setViewMode = setViewMode;
window.applyPersistedViewModes = applyPersistedViewModes;
window.setTheme = setTheme;
window.toggleTheme = toggleTheme;
