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

        const taskCard = event.target.closest(".day-task-card");
        if (taskCard && !isCalendarInteractiveElement(event.target)) {
            toggleCalendarTaskDetails(taskCard);
            return;
        }

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
        if (!prioritySelect) {
            return;
        }

        const taskCard = prioritySelect.closest(".day-task-card");
        if (!taskCard) {
            return;
        }

        closeCalendarPriorityEditor(taskCard, prioritySelect.value);
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

        window.setTimeout(() => {
            if (!taskCard.contains(document.activeElement)) {
                closeCalendarPriorityEditor(taskCard);
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

        if (form.matches(".calendar-task-delete-form")) {
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

//??????? ??????? ?? ???????? ?????? ? Calendar ? ?????????? ???? ?????? ?????
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

//??????? add/delete/done action ?? ???????? ?????? ? ???????? task cards ??????
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

    await reloadCalendarDayTasksContent(form.matches(".calendar-task-done-form") ? result.taskId : null);
}

//?????????? ???? ???????????? ??? ???????? ?? ???????? ??? ? Calendar
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

    document.querySelectorAll(".task-subtasks-panel[data-task-panel-id]").forEach(panel => {
        refreshDependencyOptions(panel.dataset.taskPanelId);
    });

    syncSelectedDayTaskDot();
}

//??????? ???????? ????????? ?? inline ???????? ? select-? ?? ?????????
function saveCalendarTaskChanges(taskCard) {
    const editForm = taskCard.querySelector(".calendar-task-update-form");
    if (!(editForm instanceof HTMLFormElement)) {
        return;
    }

    const titleValue = normalizeEditableValue(taskCard.querySelector('[data-calendar-field="title"]'));
    const descriptionValue = normalizeEditableValue(taskCard.querySelector('[data-calendar-field="description"]')) || CALENDAR_DEFAULT_DESCRIPTION;
    const prioritySelect = taskCard.querySelector(".calendar-priority-select");

    const titleInput = editForm.querySelector('input[name="Title"]');
    const descriptionInput = editForm.querySelector('input[name="Description"]');
    const priorityInput = editForm.querySelector('input[name="Priority"]');

    if (titleInput) {
        titleInput.value = titleValue;
    }

    if (descriptionInput) {
        descriptionInput.value = descriptionValue;
    }

    if (priorityInput && prioritySelect) {
        priorityInput.value = prioritySelect.value;
    }

    submitCalendarTaskUpdate(editForm);
}

//?????? inline ????????? ?? ?????????? ?? ???????? ??????
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

//??????? ????????? editor-? ? ????? ???????? ????????
function closeCalendarPriorityEditor(taskCard, selectedValue) {
    taskCard.classList.remove("priority-editing");

    const prioritySelect = taskCard.querySelector(".calendar-priority-select");
    const priorityInput = taskCard.querySelector('.calendar-task-update-form input[name="Priority"]');
    if (prioritySelect) {
        prioritySelect.value = selectedValue ?? priorityInput?.value ?? prioritySelect.value;
    }
}

//????????? ???? ?????? ? ????? ???????????? ??????? ????? ? ????????
function isCalendarInteractiveElement(element) {
    return !!element.closest("button, a, form, input, select, textarea, label, .popup-actions, .subtask-actions, .subtask-edit-form, .inline-editable, .inline-dependency-trigger, .calendar-subtasks-panel-wrap, .calendar-inline-editable, .calendar-priority-trigger");
}

//??????? ??? ?????? ?????? ? ??????????? ?? ?????? ??????
function toggleCalendarTaskDetails(taskCard) {
    const shouldExpand = !taskCard.classList.contains("expanded");

    document.querySelectorAll(".day-task-card.expanded").forEach(card => {
        if (card !== taskCard) {
            card.classList.remove("expanded");
        }
    });

    taskCard.classList.toggle("expanded", shouldExpand);
}

//????? ??? ???? ??????? ?? ???????? ??? ?????? ???? ???? ??? ?????? ???? ????????? ???????
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
