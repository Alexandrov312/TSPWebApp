document.addEventListener("DOMContentLoaded", function () {
    const taskPanels = document.querySelectorAll(".task-subtasks-panel[data-task-panel-id]");
    if (taskPanels.length === 0) {
        return;
    }

    taskPanels.forEach(panel => {
        refreshDependencyOptions(panel.dataset.taskPanelId);
    });

    document.addEventListener("click", function (event) {
        const dependencyTrigger = event.target.closest(".inline-dependency-trigger");
        if (dependencyTrigger) {
            event.preventDefault();
            event.stopPropagation();
            const currentEntry = dependencyTrigger.closest(".subtask-entry");
            if (currentEntry) {
                openDependencyEditor(currentEntry);
            }
            return;
        }

        const subtaskToggleButton = event.target.closest("[data-subtask-toggle]");
        if (subtaskToggleButton) {
            event.preventDefault();
            event.stopPropagation();
            const subtaskEntry = subtaskToggleButton.closest(".subtask-entry");
            if (subtaskEntry) {
                toggleSubTaskDescription(subtaskEntry);
            }
            return;
        }

        document.querySelectorAll(".subtask-entry.dependency-editing").forEach(entry => {
            if (!entry.contains(event.target)) {
                closeDependencyEditor(entry);
            }
        });
    });

    document.addEventListener("focusin", function (event) {
        const editableField = event.target.closest(".inline-editable");
        if (!editableField) {
            return;
        }

        editableField.dataset.originalValue = normalizeEditableValue(editableField);
    });

    document.addEventListener("keydown", function (event) {
        const editableField = event.target.closest(".inline-editable");
        if (!editableField) {
            return;
        }

        if (event.key === "Enter" && editableField.dataset.inlineField === "title") {
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
        const editableField = event.target.closest(".inline-editable");
        if (!editableField) {
            return;
        }

        const subtaskEntry = editableField.closest(".subtask-entry");
        if (!subtaskEntry) {
            return;
        }

        const originalValue = editableField.dataset.originalValue ?? "";
        const currentValue = normalizeEditableValue(editableField);

        if (editableField.dataset.inlineField === "description" && currentValue === "") {
            editableField.textContent = EMPTY_DESCRIPTION_TEXT;
        }

        if (currentValue === originalValue) {
            return;
        }

        saveSubTaskChanges(subtaskEntry, {
            title: getTitleValue(subtaskEntry),
            description: getDescriptionValue(subtaskEntry),
            blockedBySubTaskId: getDependencyValue(subtaskEntry)
        });
    });

    document.addEventListener("change", function (event) {
        const dependencySelect = event.target.closest(".subtask-inline-dependency-select");
        if (!dependencySelect) {
            return;
        }

        const subtaskEntry = dependencySelect.closest(".subtask-entry");
        if (!subtaskEntry) {
            return;
        }

        const selectedValue = dependencySelect.value;
        closeDependencyEditor(subtaskEntry, selectedValue);

        saveSubTaskChanges(subtaskEntry, {
            title: getTitleValue(subtaskEntry),
            description: getDescriptionValue(subtaskEntry),
            blockedBySubTaskId: selectedValue
        });
    });

    document.addEventListener("focusout", function (event) {
        const dependencySelect = event.target.closest(".subtask-inline-dependency-select");
        if (!dependencySelect) {
            return;
        }

        const subtaskEntry = dependencySelect.closest(".subtask-entry");
        if (!subtaskEntry) {
            return;
        }

        window.setTimeout(() => {
            if (!subtaskEntry.contains(document.activeElement)) {
                closeDependencyEditor(subtaskEntry);
            }
        }, 0);
    });

    document.addEventListener("submit", function (event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        if (form.matches(".subtask-cycle-form")) {
            event.preventDefault();
            cycleSubTaskStatus(form);
            return;
        }

        if (form.matches(".subtask-add-form")) {
            event.preventDefault();
            addSubTask(form);
            return;
        }

        if (form.matches(".subtask-delete-form")) {
            event.preventDefault();
            deleteSubTask(form);
        }
    });
});

//Сменя статуса на подзадача циклично чрез AJAX
async function cycleSubTaskStatus(form) {
    const result = await postForm(form);
    if (!result) {
        return;
    }

    if (!result.success) {
        showToast(result.message, result.notificationCssClass);
        return;
    }

    updateTaskProgress(
        result.taskId,
        result.completionPercentage,
        result.projectedCompletionPercentage,
        result.completedSubTaskCount,
        result.totalSubTaskCount);
    syncTaskStateAcrossViews(result);

    await reloadTaskSubTasksPanel(result.taskId);
    showToast(result.message, result.notificationCssClass);
}

//Добавя нова подзадача към избраната главна задача
async function addSubTask(form) {
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

    updateTaskProgress(
        result.taskId,
        result.completionPercentage,
        result.projectedCompletionPercentage,
        result.completedSubTaskCount,
        result.totalSubTaskCount);
    syncTaskStateAcrossViews(result);

    await reloadTaskSubTasksPanel(result.taskId);
}

//Изтрива подзадача и обновява панела след успех
async function deleteSubTask(form) {
    if (typeof hideAllTaskPopups === "function") {
        hideAllTaskPopups();
    }

    const result = await postForm(form);
    if (!result) {
        return;
    }

    showToast(result.message, result.notificationCssClass);
    if (!result.success) {
        return;
    }

    updateTaskProgress(
        result.taskId,
        result.completionPercentage,
        result.projectedCompletionPercentage,
        result.completedSubTaskCount,
        result.totalSubTaskCount);
    syncTaskStateAcrossViews(result);

    await reloadTaskSubTasksPanel(result.taskId);
}

//Записва inline редакция на подзадача
async function saveSubTaskChanges(subtaskEntry, payload) {
    if (subtaskEntry.dataset.isSaving === "true") {
        return;
    }

    subtaskEntry.dataset.isSaving = "true";

    const editForm = subtaskEntry.querySelector(".subtask-edit-form");
    if (!(editForm instanceof HTMLFormElement)) {
        subtaskEntry.dataset.isSaving = "false";
        return;
    }

    const formData = new FormData(editForm);
    formData.set("Title", payload.title);
    formData.set("Description", payload.description);
    formData.set("BlockedBySubTaskId", payload.blockedBySubTaskId ?? "");

    const result = await postForm(editForm, formData);
    subtaskEntry.dataset.isSaving = "false";

    if (!result) {
        return;
    }

    if (!result.success) {
        showToast(result.message, result.notificationCssClass);
        await reloadTaskSubTasksPanel(subtaskEntry.dataset.taskId);
        return;
    }

    updateTaskProgress(
        result.taskId,
        result.completionPercentage,
        result.projectedCompletionPercentage,
        result.completedSubTaskCount,
        result.totalSubTaskCount);
    syncTaskStateAcrossViews(result);

    await reloadTaskSubTasksPanel(result.taskId);
    showToast(result.message, result.notificationCssClass);
}

//Отваря select-а за редакция на dependency
function openDependencyEditor(subtaskEntry) {
    if (subtaskEntry.dataset.taskId) {
        refreshDependencyOptions(subtaskEntry.dataset.taskId);
    }

    document.querySelectorAll(".subtask-entry.dependency-editing").forEach(entry => {
        if (entry !== subtaskEntry) {
            closeDependencyEditor(entry);
        }
    });

    subtaskEntry.classList.add("dependency-editing");
    const dependencySelect = subtaskEntry.querySelector(".subtask-inline-dependency-select");
    if (dependencySelect) {
        dependencySelect.focus();
        dependencySelect.click();
    }
}

//Затваря dependency editor-а и възстановява текущата стойност
function closeDependencyEditor(subtaskEntry, selectedValue) {
    subtaskEntry.classList.remove("dependency-editing");

    const dependencySelect = subtaskEntry.querySelector(".subtask-inline-dependency-select");
    const hiddenDependency = subtaskEntry.querySelector('input[name="BlockedBySubTaskId"]');
    if (dependencySelect) {
        dependencySelect.value = selectedValue ?? hiddenDependency?.value ?? "";
    }

    if (hiddenDependency && selectedValue !== undefined) {
        hiddenDependency.value = selectedValue ?? "";
    }
}

//Взима текущото заглавие от contenteditable полето
function getTitleValue(subtaskEntry) {
    const titleElement = subtaskEntry.querySelector('[data-inline-field="title"]');
    return normalizeEditableValue(titleElement);
}

//Взима текущото описание и го връща празно при placeholder текст
function getDescriptionValue(subtaskEntry) {
    const descriptionElement = subtaskEntry.querySelector('[data-inline-field="description"]');
    const value = normalizeEditableValue(descriptionElement);
    return value === EMPTY_DESCRIPTION_TEXT ? "" : value;
}

//Връща текущия dependency id от hidden полето
function getDependencyValue(subtaskEntry) {
    const hiddenDependency = subtaskEntry.querySelector('input[name="BlockedBySubTaskId"]');
    return hiddenDependency?.value ?? "";
}
