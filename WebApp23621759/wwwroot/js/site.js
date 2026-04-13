// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const popupViewportMargin = 12;
const popupOffset = 10;

//При зареждане на страницата се изпълнява тази функция
document.addEventListener("DOMContentLoaded", function () {
    //Намира всички елементи в страницата, които имат клас .toast-notification
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

//Слуша за всеки клик
document.addEventListener("click", function (event) {

    // DONE POPUP
    if (event.target.closest(".show-done-popup")) {
        event.preventDefault();
        hideAllPopups();

        const wrapper = event.target.closest(".done-wrapper");
        const popup = wrapper.querySelector(".done-popup");

        wrapper.classList.add("popup-open");
        popup.classList.add("show");
        positionPopup(wrapper, popup);
        return;
    }

    // cancel done
    if (event.target.closest(".cancel-done-btn")) {
        const popup = event.target.closest(".done-popup");
        const wrapper = event.target.closest(".done-wrapper");

        if (popup) {
            popup.classList.remove("show");
        }

        if (wrapper) {
            wrapper.classList.remove("popup-open");
        }

        return;
    }

    // DELETE POPUP
    if (event.target.closest(".show-popup")) {
        event.preventDefault();
        hideAllPopups();

        const wrapper = event.target.closest(".delete-wrapper");
        const popup = wrapper.querySelector(".delete-popup");
        const subtaskEntry = wrapper.closest(".subtask-entry");

        wrapper.classList.add("popup-open");
        subtaskEntry?.classList.add("popup-active");
        popup.classList.add("show");
        positionPopup(wrapper, popup);
        return;
    }

    // cancel delete
    if (event.target.closest(".cancel-btn")) {
        const popup = event.target.closest(".delete-popup");
        const wrapper = event.target.closest(".delete-wrapper");
        const subtaskEntry = wrapper?.closest(".subtask-entry");

        if (popup) {
            popup.classList.remove("show");
        }

        if (wrapper) {
            wrapper.classList.remove("popup-open");
        }

        subtaskEntry?.classList.remove("popup-active");

        return;
    }

    // click outside popups
    if (!event.target.closest(".delete-wrapper") && !event.target.closest(".done-wrapper")) {
        hideAllPopups();
    }

    // edit
    if (event.target.closest(".edit-btn")) {
        const currentRow = event.target.closest(".task-row");

        if (!currentRow) {
            return;
        }

        document.querySelectorAll(".task-row.editing").forEach(row => {
            if (row !== currentRow) {
                row.classList.remove("editing");
                resetRowInputs(row);
            }
        });

        currentRow.classList.add("editing");
        return;
    }

    // cancel edit
    if (event.target.closest(".cancel-edit-btn")) {
        const row = event.target.closest(".task-row");

        if (!row) {
            return;
        }

        row.classList.remove("editing");
        resetRowInputs(row);
        return;
    }
});

document.addEventListener("submit", async function (event) {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.matches(".task-ajax-form")) {
        return;
    }

    event.preventDefault();

    const response = await fetch(form.action, {
        method: "POST",
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        body: new FormData(form)
    });

    if (!response.ok) {
        showToastMessage("Task request failed.", "toast-error");
        return;
    }

    const result = await response.json();
    hideAllPopups();
    showToastMessage(result.message, result.notificationCssClass);

    if (!result.success) {
        return;
    }

    if (form.matches(".task-delete-form")) {
        removeTaskRow(result.taskId);
        return;
    }

    if (form.matches(".task-done-form")) {
        applyTaskDoneResult(result);
        return;
    }

    if (form.matches(".task-update-form")) {
        applyTaskUpdateResult(result);
    }
});

window.addEventListener("resize", function () {
    repositionVisiblePopups();
});

window.addEventListener("scroll", function () {
    repositionVisiblePopups();
}, true);

//Връща input/select стойностите към оригиналните
function resetRowInputs(row) {
    const inputs = row.querySelectorAll("input.edit-mode");
    inputs.forEach(input => {
        input.value = input.defaultValue;
    });

    const selects = row.querySelectorAll("select.edit-mode");
    selects.forEach(select => {
        Array.from(select.options).forEach(option => {
            option.selected = option.defaultSelected;
        });
    });
}

function hideAllPopups() {
    document.querySelectorAll(".delete-popup, .done-popup")
        .forEach(popup => {
            popup.classList.remove("show");
            popup.style.top = "";
            popup.style.left = "";
        });

    document.querySelectorAll(".delete-wrapper, .done-wrapper")
        .forEach(wrapper => wrapper.classList.remove("popup-open"));

    document.querySelectorAll(".subtask-entry.popup-active")
        .forEach(entry => entry.classList.remove("popup-active"));
}

function repositionVisiblePopups() {
    document.querySelectorAll(".done-wrapper.popup-open, .delete-wrapper.popup-open")
        .forEach(wrapper => {
            const popup = wrapper.querySelector(".done-popup.show, .delete-popup.show");
            if (popup) {
                positionPopup(wrapper, popup);
            }
        });
}

function positionPopup(wrapper, popup) {
    const wrapperRect = wrapper.getBoundingClientRect();
    const popupRect = popup.getBoundingClientRect();

    let top = wrapperRect.bottom + popupOffset;
    if (top + popupRect.height > window.innerHeight - popupViewportMargin) {
        top = wrapperRect.top - popupRect.height - popupOffset;
    }

    top = Math.max(popupViewportMargin, top);

    let left = wrapperRect.right - popupRect.width;
    const maxLeft = window.innerWidth - popupRect.width - popupViewportMargin;
    left = Math.min(Math.max(popupViewportMargin, left), Math.max(popupViewportMargin, maxLeft));

    popup.style.top = `${top}px`;
    popup.style.left = `${left}px`;
}

function removeTaskRow(taskId) {
    const summaryRow = document.querySelector(`.task-summary-row[data-task-row-id="${taskId}"]`);
    const detailsRow = document.querySelector(`.subtasks-row[data-task-details-id="${taskId}"]`);

    summaryRow?.remove();
    detailsRow?.remove();
}

function applyTaskDoneResult(result) {
    applyTaskUpdateState(result.taskId, {
        statusDisplayName: result.statusDisplayName,
        statusValue: result.statusValue,
        statusCssClass: result.statusCssClass,
        completedAtText: result.completedAtText,
        rowDateCssClass: result.rowDateCssClass,
        completionPercentage: result.completionPercentage,
        projectedCompletionPercentage: result.projectedCompletionPercentage,
        completedSubTaskCount: result.completedSubTaskCount,
        totalSubTaskCount: result.totalSubTaskCount
    });

    if (typeof window.reloadTaskSubTasksPanel === "function") {
        window.reloadTaskSubTasksPanel(result.taskId);
    }
}

function applyTaskUpdateResult(result) {
    const summaryRow = document.querySelector(`.task-summary-row[data-task-row-id="${result.taskId}"]`);
    if (!summaryRow) {
        return;
    }

    const titleCell = summaryRow.querySelector('[data-task-field="title"] .view-mode');
    const descriptionCell = summaryRow.querySelector('[data-task-field="description"] .view-mode');
    const dueDateCell = summaryRow.querySelector('[data-task-field="dueDate"] .view-mode');
    const statusCell = summaryRow.querySelector('[data-task-field="status"] .view-mode');
    const priorityBadge = summaryRow.querySelector('[data-task-field="priority"] .priority-badge');
    const completedAtCell = summaryRow.querySelector('[data-task-field="completedAt"] .task-cell-display');
    const titleInput = summaryRow.querySelector('input[name="Title"]');
    const descriptionInput = summaryRow.querySelector('input[name="Description"]');
    const dueDateInput = summaryRow.querySelector('input[name="DueDate"]');
    const statusSelect = summaryRow.querySelector('select[name="Status"]');
    const prioritySelect = summaryRow.querySelector('select[name="Priority"]');

    if (titleCell) titleCell.textContent = result.title;
    if (descriptionCell) descriptionCell.textContent = result.description;
    if (dueDateCell) dueDateCell.textContent = result.dueDateText;
    if (statusCell) statusCell.textContent = result.statusDisplayName;
    if (completedAtCell) completedAtCell.textContent = result.completedAtText;

    if (priorityBadge) {
        priorityBadge.textContent = result.priorityDisplayName;
        priorityBadge.className = `priority-badge ${result.priorityCssClass}`;
    }

    if (titleInput) {
        titleInput.value = result.title;
        titleInput.defaultValue = result.title;
    }

    if (descriptionInput) {
        descriptionInput.value = result.description;
        descriptionInput.defaultValue = result.description;
    }

    if (dueDateInput) {
        dueDateInput.value = result.dueDateValue;
        dueDateInput.defaultValue = result.dueDateValue;
    }

    if (statusSelect) {
        statusSelect.value = String(result.statusValue);
        Array.from(statusSelect.options).forEach(option => {
            option.defaultSelected = option.value === String(result.statusValue);
        });
    }

    if (prioritySelect) {
        prioritySelect.value = String(result.priorityValue);
        Array.from(prioritySelect.options).forEach(option => {
            option.defaultSelected = option.value === String(result.priorityValue);
        });
    }

    summaryRow.classList.remove("editing");
    applyTaskUpdateState(result.taskId, result);
}

function applyTaskUpdateState(taskId, result) {
    const summaryRow = document.querySelector(`.task-summary-row[data-task-row-id="${taskId}"]`);
    const progressContainer = document.querySelector(`.task-progress[data-task-progress-id="${taskId}"]`);
    const rowDateClasses = ["task-row-due-today", "task-row-overdue", "task-row-completed-past", "task-row-upcoming"];
    const statusClasses = ["status-pending", "status-in-progress", "status-completed", "status-overdue"];
    const statusCell = summaryRow?.querySelector('[data-task-field="status"] .view-mode');
    const completedAtCell = summaryRow?.querySelector('[data-task-field="completedAt"] .task-cell-display');

    if (summaryRow) {
        summaryRow.classList.remove(...rowDateClasses, ...statusClasses);
        if (result.rowDateCssClass) {
            summaryRow.classList.add(result.rowDateCssClass);
        }
        if (result.statusCssClass) {
            summaryRow.classList.add(result.statusCssClass);
        }
    }

    if (statusCell && result.statusDisplayName) {
        statusCell.textContent = result.statusDisplayName;
    }

    if (completedAtCell && result.completedAtText) {
        completedAtCell.textContent = result.completedAtText;
    }

    if (progressContainer && Number.isFinite(result.completionPercentage)) {
        const progressFill = progressContainer.querySelector(".task-progress-fill-completed");
        const projectedProgressFill = progressContainer.querySelector(".task-progress-fill-projected");
        const progressText = progressContainer.querySelector(".task-progress-text");

        if (progressFill) {
            progressFill.style.width = `${result.completionPercentage}%`;
        }

        if (projectedProgressFill) {
            const projectedWidth = Math.max(result.projectedCompletionPercentage ?? result.completionPercentage, result.completionPercentage);
            projectedProgressFill.style.width = `${projectedWidth}%`;
        }

        if (progressText) {
            progressText.textContent = `${result.completionPercentage}% (${result.completedSubTaskCount}/${result.totalSubTaskCount})`;
        }
    }
}

function showToastMessage(message, cssClass) {
    if (typeof window.showToast === "function") {
        window.showToast(message, cssClass);
        return;
    }

    const container = document.getElementById("toast-container") ?? document.body.appendChild(Object.assign(document.createElement("div"), { id: "toast-container" }));
    const toast = document.createElement("div");
    toast.className = `toast-notification ${cssClass || "toast-info"}`;
    toast.textContent = message;
    container.appendChild(toast);
}
