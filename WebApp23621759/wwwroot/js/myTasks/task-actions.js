const popupViewportMargin = 12;
const popupOffset = 10;
let popupSequence = 0;

document.addEventListener("DOMContentLoaded", function () {
    document.addEventListener("click", function (event) {
        const taskRow = event.target.closest(".task-summary-row");
        if (taskRow && !isTaskInteractiveElement(event.target)) {
            toggleTaskDetails(taskRow);
            return;
        }

        const gridEditTrigger = event.target.closest(".task-grid-inline-trigger");
        if (gridEditTrigger) {
            event.preventDefault();
            event.stopPropagation();
            openTaskGridInlineEdit(gridEditTrigger);
            return;
        }

        const taskGridCard = event.target.closest(".task-grid-card");
        if (taskGridCard && !isTaskInteractiveElement(event.target)) {
            toggleTaskGridDetails(taskGridCard.dataset.taskGridId);
            return;
        }

        const descriptionToggle = event.target.closest("[data-task-description-toggle]");
        if (descriptionToggle) {
            event.preventDefault();
            const wrapper = descriptionToggle.closest(".task-details-description");
            const isOpen = wrapper?.classList.toggle("description-open");
            descriptionToggle.setAttribute("aria-expanded", String(!!isOpen));
            return;
        }

        if (event.target.closest(".show-done-popup")) {
            event.preventDefault();
            hideAllTaskPopups();

            const wrapper = event.target.closest(".done-wrapper");
            const popup = wrapper.querySelector(".done-popup");
            openTaskPopup(wrapper, popup);
            return;
        }

        if (event.target.closest(".show-popup")) {
            event.preventDefault();
            hideAllTaskPopups();

            const wrapper = event.target.closest(".delete-wrapper");
            const popup = wrapper.querySelector(".delete-popup");
            openTaskPopup(wrapper, popup);
            return;
        }

        const popupSubmitButton = event.target.closest(".delete-popup button[type='submit'], .done-popup button[type='submit']");
        if (popupSubmitButton) {
            event.preventDefault();
            const wrapper = getWrapperFromPopupEvent(event.target, ".delete-wrapper") ?? getWrapperFromPopupEvent(event.target, ".done-wrapper");
            const form = wrapper?.querySelector("form");
            if (form instanceof HTMLFormElement) {
                form.requestSubmit();
            }
            return;
        }

        if (event.target.closest(".cancel-done-btn")) {
            closeTaskPopup(getWrapperFromPopupEvent(event.target, ".done-wrapper"));
            return;
        }

        if (event.target.closest(".cancel-btn")) {
            closeTaskPopup(getWrapperFromPopupEvent(event.target, ".delete-wrapper"));
            return;
        }

        if (!event.target.closest(".delete-wrapper") && !event.target.closest(".done-wrapper") && !event.target.closest(".delete-popup") && !event.target.closest(".done-popup")) {
            hideAllTaskPopups();
        }

        if (event.target.closest(".edit-btn")) {
            const currentTaskElement = event.target.closest(".task-row, .task-grid-card");
            if (!currentTaskElement) {
                return;
            }

            //Позволява само един главен ред да е в edit mode по едно и също време
            document.querySelectorAll(".task-row.editing, .task-grid-card.editing").forEach(element => {
                if (element !== currentTaskElement) {
                    element.classList.remove("editing", "editing-title", "editing-dueDate", "editing-priority");
                    resetTaskRowInputs(element);
                }
            });

            currentTaskElement.classList.add("editing");
            return;
        }

        if (event.target.closest(".cancel-edit-btn")) {
            const taskElement = event.target.closest(".task-row, .task-grid-card");
            if (!taskElement) {
                return;
            }

            taskElement.classList.remove("editing", "editing-title", "editing-dueDate", "editing-priority");
            resetTaskRowInputs(taskElement);
        }
    });

    document.addEventListener("submit", async function (event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.matches(".task-ajax-form")) {
            return;
        }

        event.preventDefault();

        if (form.dataset.isSubmitting === "true") {
            return;
        }

        form.dataset.isSubmitting = "true";
        try {
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

            if (form.matches(".task-archive-form")) {
                removeTaskRow(result.taskId);
                return;
            }

            if (form.matches(".task-done-form, .task-pending-form")) {
                applyTaskStatusActionResult(result);
                return;
            }

            if (form.matches(".task-update-form")) {
                applyTaskUpdateResult(result);
            }
        } finally {
            delete form.dataset.isSubmitting;
        }
    });

    document.addEventListener("focusin", function (event) {
        const editableDescription = event.target.closest("[data-task-description-editable]");
        if (editableDescription) {
            editableDescription.dataset.originalValue = normalizeEditableValue(editableDescription);
        }
    });

    document.addEventListener("keydown", function (event) {
        const gridInput = event.target.closest(".task-grid-card.editing input.edit-mode, .task-grid-card.editing select.edit-mode");
        if (gridInput) {
            if (event.key === "Enter") {
                event.preventDefault();
                saveTaskGridInlineEdit(gridInput);
            }

            if (event.key === "Escape") {
                event.preventDefault();
                const card = gridInput.closest(".task-grid-card");
                resetTaskRowInputs(card);
                card?.classList.remove("editing", "editing-title", "editing-dueDate", "editing-priority");
            }
        }

        const editableDescription = event.target.closest("[data-task-description-editable]");
        if (!editableDescription) {
            return;
        }

        if (event.key === "Enter") {
            event.preventDefault();
            editableDescription.blur();
        }

        if (event.key === "Escape") {
            event.preventDefault();
            editableDescription.textContent = editableDescription.dataset.originalValue ?? editableDescription.textContent ?? "";
            editableDescription.blur();
        }
    });

    document.addEventListener("focusout", async function (event) {
        const gridInput = event.target.closest(".task-grid-card.editing input.edit-mode");
        if (gridInput) {
            saveTaskGridInlineEdit(gridInput);
            return;
        }

        const gridSelect = event.target.closest(".task-grid-card.editing select.edit-mode");
        if (gridSelect) {
            window.setTimeout(() => {
                const card = gridSelect.closest(".task-grid-card");
                const isChanged = Array.from(gridSelect.options).some(option => option.selected !== option.defaultSelected);
                if (card?.classList.contains("editing") && !isChanged) {
                    resetTaskRowInputs(card);
                    card.classList.remove("editing", "editing-title", "editing-dueDate", "editing-priority");
                }
            }, 0);
            return;
        }

        const editableDescription = event.target.closest("[data-task-description-editable]");
        if (!editableDescription) {
            return;
        }

        const originalValue = editableDescription.dataset.originalValue ?? "";
        const currentValue = normalizeEditableValue(editableDescription) || "Task description";
        if (currentValue === originalValue) {
            return;
        }

        await saveTaskDetailsDescription(editableDescription, currentValue);
    });

    document.addEventListener("change", function (event) {
        const gridInput = event.target.closest(".task-grid-card.editing input.edit-mode, .task-grid-card.editing select.edit-mode");
        if (gridInput) {
            saveTaskGridInlineEdit(gridInput);
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
    return !!element.closest("button, a, form, input, select, textarea, label, .popup-actions, .subtask-actions, .subtask-edit-form, .inline-editable, .inline-dependency-trigger, [data-task-description-editable], .task-grid-details");
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

//Запазва автоматично конкретната inline редакция в grid card-а
function saveTaskGridInlineEdit(input) {
    const card = input.closest(".task-grid-card");
    const form = card?.querySelector(".task-update-form");
    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    const isSelectChanged = input instanceof HTMLSelectElement
        && Array.from(input.options).some(option => option.selected !== option.defaultSelected);
    const isInputChanged = input instanceof HTMLInputElement
        && input.value !== input.defaultValue;

    if (!isSelectChanged && !isInputChanged) {
        card.classList.remove("editing", "editing-title", "editing-dueDate", "editing-priority");
        return;
    }

    form.requestSubmit();
}

//Затваря всички активни popup-и за задачите
function hideAllTaskPopups() {
    document.querySelectorAll(".delete-popup, .done-popup")
        .forEach(popup => {
            popup.classList.remove("show");
            popup.style.top = "-9999px";
            popup.style.left = "-9999px";
            popup.style.visibility = "";
            restorePopupParent(popup);
        });

    document.querySelectorAll(".delete-wrapper, .done-wrapper")
        .forEach(wrapper => wrapper.classList.remove("popup-open"));
}

//Затваря конкретен popup wrapper и изчиства състоянието му
function closeTaskPopup(wrapper) {
    const popup = getPopupForWrapper(wrapper);

    if (popup) {
        popup.classList.remove("show");
        popup.style.top = "-9999px";
        popup.style.left = "-9999px";
        popup.style.visibility = "";
        restorePopupParent(popup);
    }

    wrapper?.classList.remove("popup-open");
}

//Препозиционира всички отворени popup-и при scroll или resize
function repositionVisibleTaskPopups() {
    document.querySelectorAll(".done-wrapper.popup-open, .delete-wrapper.popup-open")
        .forEach(wrapper => {
            const popup = getPopupForWrapper(wrapper);
            if (popup) {
                positionTaskPopup(wrapper, popup);
            }
        });
}

//Отваря popup-а в body, за да не бъде ограничен от таблици, overflow или stacking context-и
function openTaskPopup(wrapper, popup) {
    if (!wrapper || !popup) {
        return;
    }

    wrapper.classList.add("popup-open");
    preparePopupPortal(wrapper, popup);

    popup.style.visibility = "hidden";
    popup.classList.add("show");
    positionTaskPopup(wrapper, popup);
    popup.style.visibility = "visible";
}

//Свързва popup-а с wrapper-а и с формата, дори когато popup-ът е преместен извън нея
function preparePopupPortal(wrapper, popup) {
    const popupId = wrapper.dataset.popupPortalId || `task-popup-${++popupSequence}`;
    wrapper.dataset.popupPortalId = popupId;
    popup.dataset.popupPortalId = popupId;

    if (!popup.originalParent) {
        popup.originalParent = popup.parentElement;
    }

    const form = wrapper.querySelector("form");
    if (form) {
        form.id ||= `${popupId}-form`;
        popup.querySelectorAll('button[type="submit"]').forEach(button => {
            button.setAttribute("form", form.id);
        });
    }

    document.body.appendChild(popup);
}

//Връща popup-а обратно в първоначалния му wrapper, когато се затвори
function restorePopupParent(popup) {
    if (popup?.originalParent && popup.parentElement !== popup.originalParent) {
        popup.originalParent.appendChild(popup);
    }
}

//Намира popup-а дори когато временно е преместен в body
function getPopupForWrapper(wrapper) {
    if (!wrapper) {
        return null;
    }

    const popupId = wrapper.dataset.popupPortalId;
    if (popupId) {
        return document.querySelector(`.done-popup[data-popup-portal-id="${popupId}"], .delete-popup[data-popup-portal-id="${popupId}"]`);
    }

    return wrapper.querySelector(".done-popup, .delete-popup");
}

//Намира wrapper-а при клик в popup, който вече може да е извън формата
function getWrapperFromPopupEvent(element, fallbackSelector) {
    const directWrapper = element.closest(fallbackSelector);
    if (directWrapper) {
        return directWrapper;
    }

    const popup = element.closest(".done-popup, .delete-popup");
    const popupId = popup?.dataset.popupPortalId;
    return popupId
        ? document.querySelector(`.done-wrapper[data-popup-portal-id="${popupId}"], .delete-wrapper[data-popup-portal-id="${popupId}"]`)
        : null;
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
    document.querySelectorAll(`.task-summary-row[data-task-row-id="${taskId}"], .subtasks-row[data-task-details-id="${taskId}"], .task-grid-card[data-task-grid-id="${taskId}"], .task-grid-details[data-task-grid-details-id="${taskId}"]`)
        .forEach(element => element.remove());
}

//Отваря/затваря детайлите на task card в grid режима
function toggleTaskGridDetails(taskId) {
    const card = document.querySelector(`.task-grid-card[data-task-grid-id="${taskId}"]`);
    const details = document.querySelector(`.task-grid-details[data-task-grid-details-id="${taskId}"]`);
    if (!card || !details) {
        return;
    }

    const shouldOpen = !details.classList.contains("open");
    document.querySelectorAll(".task-grid-card.expanded").forEach(openCard => {
        if (openCard !== card) {
            openCard.classList.remove("expanded");
        }
    });

    document.querySelectorAll(".task-grid-details.open").forEach(openDetails => {
        if (openDetails !== details) {
            openDetails.classList.remove("open");
        }
    });

    if (shouldOpen) {
        moveGridDetailsAfterCurrentRow(card, details, ".task-grid-card");
    }

    card.classList.toggle("expanded", shouldOpen);
    details.classList.toggle("open", shouldOpen);
}

//Премества details панела след последната карта от същия визуален ред
function moveGridDetailsAfterCurrentRow(card, details, cardSelector) {
    const grid = card.parentElement;
    if (!grid) {
        return;
    }

    const rowTop = card.offsetTop;
    const rowCards = Array.from(grid.querySelectorAll(cardSelector))
        .filter(candidate => candidate.offsetTop === rowTop);
    const lastCardInRow = rowCards[rowCards.length - 1] ?? card;
    lastCardInRow.insertAdjacentElement("afterend", details);
}

//Отваря edit mode от самия текст в grid card-а и фокусира правилното поле
function openTaskGridInlineEdit(trigger) {
    const card = trigger.closest(".task-grid-card");
    if (!card) {
        return;
    }

    document.querySelectorAll(".task-grid-card.editing").forEach(openCard => {
        if (openCard !== card) {
            openCard.classList.remove("editing", "editing-title", "editing-dueDate", "editing-priority");
            resetTaskRowInputs(openCard);
        }
    });

    const target = trigger.dataset.gridEditTarget;
    const selectorByTarget = {
        title: 'input[name="Title"]',
        dueDate: 'input[name="DueDate"]',
        priority: 'select[name="Priority"]'
    };

    card.classList.remove("editing-title", "editing-dueDate", "editing-priority");
    card.classList.add("editing", `editing-${target}`);

    const editable = card.querySelector(selectorByTarget[target]);
    if (editable) {
        editable.focus();
        if (editable.select) {
            editable.select();
        }
        if (target === "dueDate" && typeof editable.showPicker === "function") {
            editable.showPicker();
        }
    }
}

//Запазва inline редакцията на описанието от details панела на главната задача
async function saveTaskDetailsDescription(editableDescription, description) {
    const taskId = editableDescription.dataset.taskId;
    const summaryRow = document.querySelector(`.task-summary-row[data-task-row-id="${taskId}"]`);
    const form = summaryRow?.querySelector(".task-update-form");
    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    const descriptionInput = summaryRow.querySelector('input[name="Description"]');
    if (descriptionInput) {
        descriptionInput.value = description;
    }

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
    showToast(result.message, result.notificationCssClass);
    if (result.success) {
        applyTaskUpdateResult(result);
    }
}

//Обновява интерфейса след успешно маркиране на главна задача като готова или неготова
function applyTaskStatusActionResult(result) {
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

    if (document.querySelector(`.mytasks-kanban-board[data-kanban-task-id="${result.taskId}"]`) && typeof reloadMyTasksKanbanBoard === "function") {
        reloadMyTasksKanbanBoard(result.taskId);
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

    const detailsDescriptions = document.querySelectorAll(`[data-task-description-editable][data-task-id="${result.taskId}"]`);
    detailsDescriptions.forEach(detailsDescription => {
        detailsDescription.textContent = result.description || "Task description";
        detailsDescription.dataset.originalValue = detailsDescription.textContent;
    });

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

    const gridCard = document.querySelector(`.task-grid-card[data-task-grid-id="${result.taskId}"]`);
    if (gridCard) {
        const gridTitle = gridCard.querySelector(".task-grid-title-area h3.view-mode, .task-grid-card-header h3.view-mode");
        const gridPriorityBadge = gridCard.querySelector(".priority-badge");
        const gridDueText = gridCard.querySelector("[data-grid-due-text]");
        const gridCompletedText = gridCard.querySelector("[data-grid-completed-text]");
        const gridTitleInput = gridCard.querySelector('input[name="Title"]');
        const gridDescriptionInput = gridCard.querySelector('input[name="Description"]');
        const gridDueDateInput = gridCard.querySelector('input[name="DueDate"]');
        const gridPrioritySelect = gridCard.querySelector('select[name="Priority"]');
        if (gridTitle) {
            gridTitle.textContent = result.title;
        }

        if (gridPriorityBadge) {
            gridPriorityBadge.textContent = result.priorityDisplayName;
            gridPriorityBadge.className = `priority-badge view-mode task-grid-inline-trigger ${result.priorityCssClass}`;
        }

        if (gridDueText) {
            gridDueText.textContent = `Due ${result.dueDateText}`;
        }

        if (gridCompletedText) {
            gridCompletedText.textContent = result.completedAtText === "Not finished"
                ? "Not finished"
                : `Completed: ${result.completedAtText}`;
        }

        if (gridTitleInput) {
            gridTitleInput.value = result.title;
            gridTitleInput.defaultValue = result.title;
        }

        if (gridDescriptionInput) {
            gridDescriptionInput.value = result.description;
            gridDescriptionInput.defaultValue = result.description;
        }

        if (gridDueDateInput) {
            gridDueDateInput.value = result.dueDateValue;
            gridDueDateInput.defaultValue = result.dueDateValue;
        }

        if (gridPrioritySelect) {
            gridPrioritySelect.value = String(result.priorityValue);
            Array.from(gridPrioritySelect.options).forEach(option => {
                option.defaultSelected = option.value === String(result.priorityValue);
            });
        }

        gridCard.classList.remove("editing", "editing-title", "editing-dueDate", "editing-priority");
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

    if (typeof syncArchiveButton === "function") {
        syncArchiveButton(taskId, result.statusValue);
    }
}

window.hideAllTaskPopups = hideAllTaskPopups;
