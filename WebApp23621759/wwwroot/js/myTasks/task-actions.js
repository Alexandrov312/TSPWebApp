const popupViewportMargin = 12;
const popupOffset = 10;

document.addEventListener("DOMContentLoaded", function () {
    document.addEventListener("click", function (event) {
        const taskRow = event.target.closest(".task-summary-row");
        if (taskRow && !isTaskInteractiveElement(event.target)) {
            toggleTaskDetails(taskRow);
            return;
        }

        if (event.target.closest(".show-done-popup")) {
            event.preventDefault();
            hideAllTaskPopups();

            const wrapper = event.target.closest(".done-wrapper");
            const popup = wrapper.querySelector(".done-popup");
            wrapper.classList.add("popup-open");

            //Прави popup-а измерим, преди да стане видим на правилното място
            popup.style.visibility = "hidden";
            popup.classList.add("show");
            positionTaskPopup(wrapper, popup);
            popup.style.visibility = "visible";
            return;
        }

        if (event.target.closest(".show-popup")) {
            event.preventDefault();
            hideAllTaskPopups();

            const wrapper = event.target.closest(".delete-wrapper");
            const popup = wrapper.querySelector(".delete-popup");

            wrapper.classList.add("popup-open");
            popup.style.visibility = "hidden";
            popup.classList.add("show");
            positionTaskPopup(wrapper, popup);
            popup.style.visibility = "visible";
            return;
        }

        if (event.target.closest(".cancel-done-btn")) {
            closeTaskPopup(event.target.closest(".done-wrapper"));
            return;
        }

        if (event.target.closest(".cancel-btn")) {
            closeTaskPopup(event.target.closest(".delete-wrapper"));
            return;
        }

        if (!event.target.closest(".delete-wrapper") && !event.target.closest(".done-wrapper")) {
            hideAllTaskPopups();
        }

        if (event.target.closest(".edit-btn")) {
            const currentRow = event.target.closest(".task-row");
            if (!currentRow) {
                return;
            }

            //Позволява само един главен ред да е в edit mode по едно и също време
            document.querySelectorAll(".task-row.editing").forEach(row => {
                if (row !== currentRow) {
                    row.classList.remove("editing");
                    resetTaskRowInputs(row);
                }
            });

            currentRow.classList.add("editing");
            return;
        }

        if (event.target.closest(".cancel-edit-btn")) {
            const row = event.target.closest(".task-row");
            if (!row) {
                return;
            }

            row.classList.remove("editing");
            resetTaskRowInputs(row);
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
            showToast("Task request failed.", "toast-error");
            return;
        }

        const result = await response.json();
        hideAllTaskPopups();
        showToast(result.message, result.notificationCssClass);

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
});

window.addEventListener("resize", function () {
    repositionVisibleTaskPopups();
});

window.addEventListener("scroll", function () {
    repositionVisibleTaskPopups();
}, true);

//Проверява дали кликът е върху интерактивен елемент в главната задача
function isTaskInteractiveElement(element) {
    return !!element.closest("button, a, form, input, select, textarea, label, .popup-actions, .row-actions, .subtask-actions, .subtask-edit-form, .inline-editable, .inline-dependency-trigger");
}

//Връща стойностите в edit mode към първоначалните им данни
function resetTaskRowInputs(row) {
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

//Затваря всички активни popup-и за задачите
function hideAllTaskPopups() {
    document.querySelectorAll(".delete-popup, .done-popup")
        .forEach(popup => {
            popup.classList.remove("show");
            popup.style.top = "-9999px";
            popup.style.left = "-9999px";
            popup.style.visibility = "";
        });

    document.querySelectorAll(".delete-wrapper, .done-wrapper")
        .forEach(wrapper => wrapper.classList.remove("popup-open"));
}

//Затваря конкретен popup wrapper и изчиства състоянието му
function closeTaskPopup(wrapper) {
    const popup = wrapper?.querySelector(".done-popup, .delete-popup");

    if (popup) {
        popup.classList.remove("show");
        popup.style.top = "-9999px";
        popup.style.left = "-9999px";
        popup.style.visibility = "";
    }

    wrapper?.classList.remove("popup-open");
}

//Препозиционира всички отворени popup-и при scroll или resize
function repositionVisibleTaskPopups() {
    document.querySelectorAll(".done-wrapper.popup-open, .delete-wrapper.popup-open")
        .forEach(wrapper => {
            const popup = wrapper.querySelector(".done-popup.show, .delete-popup.show");
            if (popup) {
                positionTaskPopup(wrapper, popup);
            }
        });
}

//Поставя popup-а до бутона и го пази във viewport-а
function positionTaskPopup(wrapper, popup) {
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

//Премахва редовете на главната задача и детайлния ѝ ред от таблицата
function removeTaskRow(taskId) {
    const summaryRow = document.querySelector(`.task-summary-row[data-task-row-id="${taskId}"]`);
    const detailsRow = document.querySelector(`.subtasks-row[data-task-details-id="${taskId}"]`);

    summaryRow?.remove();
    detailsRow?.remove();
}

//Обновява интерфейса след успешно маркиране на главна задача като готова
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

//Попълва видимите клетки след редакция на главна задача
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

    if (titleCell) {
        titleCell.textContent = result.title;
    }
    if (descriptionCell) {
        descriptionCell.textContent = result.description;
    }
    if (dueDateCell) {
        dueDateCell.textContent = result.dueDateText;
    }
    if (statusCell) {
        statusCell.textContent = result.statusDisplayName;
    }
    if (completedAtCell) {
        completedAtCell.textContent = result.completedAtText;
    }

    if (priorityBadge) {
        priorityBadge.textContent = result.priorityDisplayName;
        priorityBadge.className = `priority-badge ${result.priorityCssClass}`;
    }

    //Обновява и default стойностите, за да работи cancel коректно след успешен save
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

//Обновява статус, completedAt, редови класове и прогрес за главната задача
function applyTaskUpdateState(taskId, result) {
    syncTaskStateAcrossViews({
        taskId,
        taskStatusValue: result.statusValue,
        taskStatusDisplayName: result.statusDisplayName,
        taskStatusCssClass: result.statusCssClass,
        taskCompletedAtText: result.completedAtText,
        taskRowDateCssClass: result.rowDateCssClass
    });

    const summaryRow = document.querySelector(`.task-summary-row[data-task-row-id="${taskId}"]`);
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

    if (Number.isFinite(result.completionPercentage)) {
        updateTaskProgress(
            taskId,
            result.completionPercentage,
            result.projectedCompletionPercentage,
            result.completedSubTaskCount,
            result.totalSubTaskCount);
    }
}

window.hideAllTaskPopups = hideAllTaskPopups;
