const EMPTY_DESCRIPTION_TEXT = "No description";

//Изпраща POST заявка чрез fetch към form.action и връща JSON отговора
async function postForm(form, body) {
    const response = await fetch(form.action, {
        method: "POST",
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        body: body ?? new FormData(form)
    });

    if (!response.ok) {
        showToast("Request failed.", "toast-error");
        return null;
    }

    return await response.json();
}

//Обновява визуално прогреса на главната задача
function updateTaskProgress(taskId, completionPercentage, projectedCompletionPercentage, completedSubTaskCount, totalSubTaskCount) {
    const progressContainer = document.querySelector(`.task-progress[data-task-progress-id="${taskId}"]`);
    if (!progressContainer) {
        return;
    }

    const completedFill = progressContainer.querySelector(".task-progress-fill-completed");
    const projectedFill = progressContainer.querySelector(".task-progress-fill-projected");
    const progressText = progressContainer.querySelector(".task-progress-text");

    if (completedFill) {
        completedFill.style.width = `${completionPercentage}%`;
    }

    if (projectedFill) {
        const projectedWidth = Math.max(projectedCompletionPercentage ?? completionPercentage, completionPercentage);
        projectedFill.style.width = `${projectedWidth}%`;
    }

    if (progressText) {
        progressText.textContent = `${completionPercentage}% (${completedSubTaskCount}/${totalSubTaskCount})`;
    }
}

//Прилага новия статус на главната задача едновременно в MyTasks и Calendar, ако съответният елемент е на екрана
function syncTaskStateAcrossViews(result) {
    if (!result?.taskId) {
        return;
    }

    const taskStatusValue = String(result.taskStatusValue ?? result.statusValue ?? "");
    const taskStatusDisplayName = result.taskStatusDisplayName ?? result.statusDisplayName ?? "";
    const taskStatusCssClass = result.taskStatusCssClass ?? result.statusCssClass ?? "";
    const taskCalendarStatusCssClass = result.taskCalendarStatusCssClass ?? "";
    const taskCompletedAtText = result.taskCompletedAtText ?? result.completedAtText ?? "";
    const taskRowDateCssClass = result.taskRowDateCssClass ?? result.rowDateCssClass ?? "";

    const summaryRow = document.querySelector(`.task-summary-row[data-task-row-id="${result.taskId}"]`);
    if (summaryRow) {
        const rowDateClasses = ["task-row-due-today", "task-row-overdue", "task-row-completed-past", "task-row-upcoming"];
        const statusClasses = ["status-pending", "status-in-progress", "status-completed", "status-overdue"];
        const statusCell = summaryRow.querySelector('[data-task-field="status"] .view-mode');
        const completedAtCell = summaryRow.querySelector('[data-task-field="completedAt"] .task-cell-display');
        const statusSelect = summaryRow.querySelector('select[name="Status"]');

        summaryRow.classList.remove(...rowDateClasses, ...statusClasses);
        if (taskRowDateCssClass) {
            summaryRow.classList.add(taskRowDateCssClass);
        }
        if (taskStatusCssClass) {
            summaryRow.classList.add(taskStatusCssClass);
        }

        if (statusCell) {
            statusCell.textContent = taskStatusDisplayName;
        }

        if (completedAtCell) {
            completedAtCell.textContent = taskCompletedAtText;
        }

        if (statusSelect && taskStatusValue) {
            statusSelect.value = taskStatusValue;
            Array.from(statusSelect.options).forEach(option => {
                option.defaultSelected = option.value === taskStatusValue;
            });
        }
    }

    const taskCard = document.querySelector(`.day-task-card[data-calendar-task-id="${result.taskId}"]`);
    if (taskCard) {
        taskCard.classList.remove("calendar-task-pending", "calendar-task-in-progress", "calendar-task-completed", "calendar-task-overdue");
        if (taskCalendarStatusCssClass) {
            taskCard.classList.add(taskCalendarStatusCssClass);
        }

        const statusText = taskCard.querySelector(".calendar-task-status-text");
        if (statusText) {
            statusText.textContent = taskStatusDisplayName;
            statusText.classList.remove("status-pending", "status-in-progress", "status-completed", "status-overdue");
            if (taskStatusCssClass) {
                statusText.classList.add(taskStatusCssClass);
            }
        }
    }

    const calendarStatusSelect = document.querySelector(`.calendar-status-form[data-task-id="${result.taskId}"] .status-select`);
    if (calendarStatusSelect) {
        if (taskStatusValue) {
            calendarStatusSelect.value = taskStatusValue;
        }

        applyStatusSelectClass(calendarStatusSelect, taskStatusCssClass);
    }
}

//Добавя правилния status-* клас към select-а на главната задача
function applyStatusSelectClass(selectElement, statusCssClass) {
    if (!selectElement) {
        return;
    }

    selectElement.classList.remove("status-pending", "status-in-progress", "status-completed", "status-overdue");
    if (statusCssClass) {
        selectElement.classList.add(statusCssClass);
    }
}

//Нормализира текст от contenteditable елемент
function normalizeEditableValue(element) {
    return element?.textContent?.replace(/\u00A0/g, " ").trim() ?? "";
}

window.postForm = postForm;
window.updateTaskProgress = updateTaskProgress;
window.syncTaskStateAcrossViews = syncTaskStateAcrossViews;
window.applyStatusSelectClass = applyStatusSelectClass;
window.normalizeEditableValue = normalizeEditableValue;
