const CALENDAR_DEFAULT_DESCRIPTION = "Task description";

document.addEventListener("DOMContentLoaded", function () {
    if (!document.querySelector(".calendar-page")) {
        return;
    }

    document.addEventListener("click", function (event) {
        const priorityTrigger = event.target.closest(".calendar-priority-trigger");
        if (priorityTrigger) {
            event.preventDefault();
            event.stopPropagation();
            const taskCard = priorityTrigger.closest(".day-task-card");
            if (taskCard) {
                openCalendarPriorityEditor(taskCard);
            }
            return;
        }

        const descriptionToggle = event.target.closest(".calendar-description-toggle");
        if (descriptionToggle) {
            event.preventDefault();
            event.stopPropagation();
            const taskCard = descriptionToggle.closest(".day-task-card");
            const isOpen = taskCard?.classList.toggle("calendar-description-open");
            descriptionToggle.setAttribute("aria-expanded", String(!!isOpen));
            return;
        }

        const dueDateTrigger = event.target.closest(".calendar-due-date-trigger");
        if (dueDateTrigger) {
            event.preventDefault();
            event.stopPropagation();
            const taskCard = dueDateTrigger.closest(".day-task-card");
            if (taskCard) {
                openCalendarDueDateEditor(taskCard);
            }
            return;
        }

        const taskCard = event.target.closest(".day-task-card");
        if (taskCard && !isCalendarInteractiveElement(event.target)) {
            toggleCalendarTaskDetails(taskCard);
            return;
        }

        //Затваря отворения priority editor, ако кликът е извън картата
        document.querySelectorAll(".day-task-card.priority-editing").forEach(card => {
            if (!card.contains(event.target)) {
                closeCalendarPriorityEditor(card);
            }
        });
    });

    document.addEventListener("focusin", function (event) {
        const editableField = event.target.closest(".calendar-inline-editable");
        if (!editableField) {
            return;
        }

        editableField.dataset.originalValue = normalizeEditableValue(editableField);
    });

    document.addEventListener("keydown", function (event) {
        const editableField = event.target.closest(".calendar-inline-editable");
        if (!editableField) {
            return;
        }

        if (event.key === "Enter") {
            event.preventDefault();
            editableField.blur();
        }

        if (event.key === "Escape") {
            event.preventDefault();
            editableField.textContent = editableField.dataset.originalValue ?? editableField.textContent ?? "";
            editableField.blur();
        }
    });

    document.addEventListener("focusout", function (event) {
        const editableField = event.target.closest(".calendar-inline-editable");
        if (!editableField) {
            return;
        }

        const taskCard = editableField.closest(".day-task-card");
        if (!taskCard) {
            return;
        }

        const originalValue = editableField.dataset.originalValue ?? "";
        let currentValue = normalizeEditableValue(editableField);

        if (editableField.dataset.calendarField === "title" && currentValue === "") {
            currentValue = originalValue;
            editableField.textContent = currentValue;
        }

        if (editableField.dataset.calendarField === "description" && currentValue === "") {
            currentValue = originalValue || CALENDAR_DEFAULT_DESCRIPTION;
            editableField.textContent = currentValue;
        }

        if (currentValue === originalValue) {
            return;
        }

        saveCalendarTaskChanges(taskCard);
    });

    document.addEventListener("change", function (event) {
        const prioritySelect = event.target.closest(".calendar-priority-select");
        if (prioritySelect) {
            const taskCard = prioritySelect.closest(".day-task-card");
            if (!taskCard) {
                return;
            }

            closeCalendarPriorityEditor(taskCard, prioritySelect.value);
            saveCalendarTaskChanges(taskCard);
            return;
        }

        const dueDateInput = event.target.closest(".calendar-due-date-input");
        if (!dueDateInput) {
            return;
        }

        const taskCard = dueDateInput.closest(".day-task-card");
        if (!taskCard) {
            return;
        }

        closeCalendarDueDateEditor(taskCard);
        saveCalendarTaskChanges(taskCard);
    });

    document.addEventListener("focusout", function (event) {
        const prioritySelect = event.target.closest(".calendar-priority-select");
        if (!prioritySelect) {
            return;
        }

        const taskCard = prioritySelect.closest(".day-task-card");
        if (!taskCard) {
            return;
        }

        //Изчаква browser-а да премести focus-а към следващия елемент
        window.setTimeout(() => {
            if (!taskCard.contains(document.activeElement)) {
                closeCalendarPriorityEditor(taskCard);
            }
        }, 0);
    });

    document.addEventListener("focusout", function (event) {
        const dueDateInput = event.target.closest(".calendar-due-date-input");
        if (!dueDateInput) {
            return;
        }

        const taskCard = dueDateInput.closest(".day-task-card");
        window.setTimeout(() => {
            if (taskCard && !taskCard.contains(document.activeElement)) {
                closeCalendarDueDateEditor(taskCard);
            }
        }, 0);
    });

    document.addEventListener("submit", function (event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        if (form.matches(".calendar-task-update-form")) {
            event.preventDefault();
            submitCalendarTaskUpdate(form);
            return;
        }

        if (form.matches(".calendar-task-done-form")) {
            event.preventDefault();
            submitCalendarTaskAction(form);
            return;
        }

        if (form.matches(".calendar-task-pending-form")) {
            event.preventDefault();
            submitCalendarTaskAction(form);
            return;
        }

        if (form.matches(".calendar-task-delete-form")) {
            event.preventDefault();
            submitCalendarTaskAction(form);
            return;
        }

        if (form.matches(".calendar-task-archive-form")) {
            event.preventDefault();
            submitCalendarTaskAction(form);
            return;
        }

        if (form.matches(".calendar-create-task-form")) {
            event.preventDefault();
            submitCalendarTaskAction(form);
        }
    });
});

//Изпраща редакция на главна задача от Calendar и презарежда десния панел
async function submitCalendarTaskUpdate(form) {
    if (form.dataset.isSubmitting === "true") {
        return;
    }

    form.dataset.isSubmitting = "true";
    const result = await postForm(form);
    form.dataset.isSubmitting = "false";
    if (!result) {
        return;
    }

    showToast(result.message, result.notificationCssClass);
    if (!result.success) {
        return;
    }

    await reloadCalendarDayTasksContent(result.taskId);
}

//Обработва add/delete/done action-и на главните задачи в Calendar
async function submitCalendarTaskAction(form) {
    if (typeof hideAllTaskPopups === "function") {
        hideAllTaskPopups();
    }

    if (form.dataset.isSubmitting === "true") {
        return;
    }

    form.dataset.isSubmitting = "true";
    const result = await postForm(form);
    form.dataset.isSubmitting = "false";
    if (!result) {
        return;
    }

    showToast(result.message, result.notificationCssClass);
    if (!result.success) {
        return;
    }

    //Запазва разгъната готовата карта, когато това е нужно след refresh
    await reloadCalendarDayTasksContent(form.matches(".calendar-task-done-form, .calendar-task-pending-form") ? result.taskId : null);
}

//Презарежда само десния панел със задачите за избрания ден в Calendar
async function reloadCalendarDayTasksContent(expandedTaskId) {
    const currentContent = document.querySelector(".calendar-day-tasks-content");
    if (!currentContent) {
        return;
    }

    const endpoint = currentContent.dataset.dayTasksEndpoint;
    if (!endpoint) {
        return;
    }

    const expandedTaskIds = expandedTaskId
        ? [String(expandedTaskId)]
        : Array.from(document.querySelectorAll(".day-task-card.expanded"))
            .map(card => card.dataset.calendarTaskId)
            .filter(Boolean);

    const response = await fetch(endpoint, {
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        }
    });

    if (!response.ok) {
        showToast("Task request failed.", "toast-error");
        return;
    }

    currentContent.outerHTML = await response.text();

    expandedTaskIds.forEach(taskId => {
        const taskCard = document.querySelector(`.day-task-card[data-calendar-task-id="${taskId}"]`);
        if (taskCard) {
            taskCard.classList.add("expanded");
        }
    });

    //След подмяна на HTML-а dependency dropdown-ите трябва да се пресметнат отново
    document.querySelectorAll(".task-subtasks-panel[data-task-panel-id]").forEach(panel => {
        refreshDependencyOptions(panel.dataset.taskPanelId);
    });

    syncSelectedDayTaskDot();
}

//Записва текущите inline стойности в hidden form-а на главната задача
function saveCalendarTaskChanges(taskCard) {
    const editForm = taskCard.querySelector(".calendar-task-update-form");
    if (!(editForm instanceof HTMLFormElement)) {
        return;
    }

    const titleValue = normalizeEditableValue(taskCard.querySelector('[data-calendar-field="title"]'));
    const descriptionValue = normalizeEditableValue(taskCard.querySelector('[data-calendar-field="description"]')) || CALENDAR_DEFAULT_DESCRIPTION;
    const prioritySelect = taskCard.querySelector(".calendar-priority-select");
    const dueDateInput = taskCard.querySelector(".calendar-due-date-input");

    const titleInput = editForm.querySelector('input[name="Title"]');
    const descriptionInput = editForm.querySelector('input[name="Description"]');
    const priorityInput = editForm.querySelector('input[name="Priority"]');
    const dueDateHiddenInput = editForm.querySelector('input[name="DueDate"]');

    if (titleInput) {
        titleInput.value = titleValue;
    }

    if (descriptionInput) {
        descriptionInput.value = descriptionValue;
    }

    if (priorityInput && prioritySelect) {
        priorityInput.value = prioritySelect.value;
    }

    if (dueDateHiddenInput && dueDateInput) {
        dueDateHiddenInput.value = dueDateInput.value;
    }

    submitCalendarTaskUpdate(editForm);
}

//Отваря inline editor-а за приоритета на главната задача
function openCalendarPriorityEditor(taskCard) {
    document.querySelectorAll(".day-task-card.priority-editing").forEach(card => {
        if (card !== taskCard) {
            closeCalendarPriorityEditor(card);
        }
    });

    taskCard.classList.add("priority-editing");
    const prioritySelect = taskCard.querySelector(".calendar-priority-select");
    if (prioritySelect) {
        prioritySelect.focus();
        prioritySelect.click();
    }
}

//Затваря priority editor-а и връща текущата избрана стойност
function closeCalendarPriorityEditor(taskCard, selectedValue) {
    taskCard.classList.remove("priority-editing");

    const prioritySelect = taskCard.querySelector(".calendar-priority-select");
    const priorityInput = taskCard.querySelector('.calendar-task-update-form input[name="Priority"]');
    if (prioritySelect) {
        prioritySelect.value = selectedValue ?? priorityInput?.value ?? prioritySelect.value;
    }
}

//Отваря inline editor-а за крайната дата на главната задача
function openCalendarDueDateEditor(taskCard) {
    document.querySelectorAll(".day-task-card.due-date-editing").forEach(card => {
        if (card !== taskCard) {
            closeCalendarDueDateEditor(card);
        }
    });

    taskCard.classList.add("due-date-editing");
    const dueDateInput = taskCard.querySelector(".calendar-due-date-input");
    if (dueDateInput) {
        dueDateInput.focus();
        if (typeof dueDateInput.showPicker === "function") {
            dueDateInput.showPicker();
        }
    }
}

//Затваря inline editor-а за крайната дата
function closeCalendarDueDateEditor(taskCard) {
    taskCard.classList.remove("due-date-editing");
}

//Проверява дали кликът е върху интерактивен елемент в картата на главна задача
function isCalendarInteractiveElement(element) {
    return !!element.closest("button, a, form, input, select, textarea, label, .popup-actions, .subtask-actions, .subtask-edit-form, .inline-editable, .inline-dependency-trigger, .calendar-subtasks-panel-wrap, .calendar-inline-editable, .calendar-priority-trigger, .calendar-description-toggle, .calendar-due-date-trigger, .calendar-due-date-input");
}

//Разгъва или свива панела с подзадачите на избраната главна задача
function toggleCalendarTaskDetails(taskCard) {
    const shouldExpand = !taskCard.classList.contains("expanded");

    document.querySelectorAll(".day-task-card.expanded").forEach(card => {
        if (card !== taskCard) {
            card.classList.remove("expanded");
        }
    });

    taskCard.classList.toggle("expanded", shouldExpand);
}

//Слага или маха task-dot на избрания ден според това дали има задачи
function syncSelectedDayTaskDot() {
    const selectedDay = document.querySelector(".calendar-day.selected");
    if (!selectedDay) {
        return;
    }

    const hasTasks = !!document.querySelector(".day-task-card");
    let taskDot = selectedDay.querySelector(".task-dot");

    if (hasTasks && !taskDot) {
        taskDot = document.createElement("span");
        taskDot.className = "task-dot";
        selectedDay.appendChild(taskDot);
    }

    if (!hasTasks && taskDot) {
        taskDot.remove();
    }
}
