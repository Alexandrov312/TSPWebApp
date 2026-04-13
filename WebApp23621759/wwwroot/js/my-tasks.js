const EMPTY_DESCRIPTION_TEXT = "No description";

document.addEventListener("DOMContentLoaded", function () {
    const taskRows = document.querySelectorAll(".task-summary-row");

    if (taskRows.length === 0) {
        return;
    }

    document.querySelectorAll(".subtasks-row[data-task-details-id]").forEach(row => {
        refreshDependencyOptions(row.dataset.taskDetailsId);
    });

    document.addEventListener("click", function (event) {
        const taskRow = event.target.closest(".task-summary-row");
        if (taskRow && !isInteractiveElement(event.target)) {
            toggleTaskDetails(taskRow);
            return;
        }

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

function toggleTaskDetails(taskRow) {
    if (taskRow.classList.contains("editing")) {
        return;
    }

    const taskId = taskRow.dataset.taskRowId;
    const detailsRow = document.querySelector(`.subtasks-row[data-task-details-id="${taskId}"]`);

    if (!detailsRow) {
        return;
    }

    const shouldOpen = !detailsRow.classList.contains("open");

    document.querySelectorAll(".task-summary-row.expanded").forEach(row => {
        if (row !== taskRow) {
            row.classList.remove("expanded");
        }
    });

    document.querySelectorAll(".subtasks-row.open").forEach(row => {
        if (row !== detailsRow) {
            row.classList.remove("open");
        }
    });

    taskRow.classList.toggle("expanded", shouldOpen);
    detailsRow.classList.toggle("open", shouldOpen);
}

function exitSubtaskEditMode(subtaskEntry) {
    closeDependencyEditor(subtaskEntry);
}

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
    await reloadTaskSubTasksPanel(result.taskId);
    showToast(result.message, result.notificationCssClass);
}

async function addSubTask(form) {
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
    await reloadTaskSubTasksPanel(result.taskId);
}

async function deleteSubTask(form) {
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
    await reloadTaskSubTasksPanel(result.taskId);
}

function isInteractiveElement(element) {
    return !!element.closest("button, a, form, input, select, textarea, label, .popup-actions, .row-actions, .subtask-actions, .subtask-edit-form, .inline-editable, .inline-dependency-trigger");
}

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
    
    await reloadTaskSubTasksPanel(result.taskId);
    showToast(result.message, result.notificationCssClass);
}

function openDependencyEditor(subtaskEntry) {
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

function closeDependencyEditor(subtaskEntry, selectedValue) {
    subtaskEntry.classList.remove("dependency-editing");

    const dependencySelect = subtaskEntry.querySelector(".subtask-inline-dependency-select");
    const hiddenDependency = subtaskEntry.querySelector('input[name="BlockedBySubTaskId"]');
    if (dependencySelect) {
        dependencySelect.value = selectedValue ?? hiddenDependency?.value ?? "";
    }
}

function toggleSubTaskDescription(subtaskEntry) {
    const shouldExpand = !subtaskEntry.classList.contains("expanded");
    setSubTaskExpanded(subtaskEntry, shouldExpand);
}

function setSubTaskExpanded(subtaskEntry, isExpanded) {
    subtaskEntry.classList.toggle("expanded", isExpanded);

    const toggleButton = subtaskEntry.querySelector("[data-subtask-toggle]");
    if (toggleButton) {
        toggleButton.setAttribute("aria-expanded", String(isExpanded));
        toggleButton.setAttribute("title", isExpanded ? "Hide description" : "Show description");
    }
}

function getTitleValue(subtaskEntry) {
    const titleElement = subtaskEntry.querySelector('[data-inline-field="title"]');
    return normalizeEditableValue(titleElement);
}

function getDescriptionValue(subtaskEntry) {
    const descriptionElement = subtaskEntry.querySelector('[data-inline-field="description"]');
    const value = normalizeEditableValue(descriptionElement);
    return value === EMPTY_DESCRIPTION_TEXT ? "" : value;
}

function getDependencyValue(subtaskEntry) {
    const hiddenDependency = subtaskEntry.querySelector('input[name="BlockedBySubTaskId"]');
    return hiddenDependency?.value ?? "";
}

function normalizeEditableValue(element) {
    return element?.textContent?.replace(/\u00A0/g, " ").trim() ?? "";
}

function showToast(message, cssClass) {
    let container = document.getElementById("toast-container");
    if (!container) {
        container = document.createElement("div");
        container.id = "toast-container";
        document.body.appendChild(container);
    }

    const toast = document.createElement("div");
    toast.className = `toast-notification ${cssClass || "toast-info"}`;
    toast.textContent = message;
    container.appendChild(toast);

    const currentToasts = container.querySelectorAll(".toast-notification");
    const index = currentToasts.length - 1;

    setTimeout(() => {
        toast.style.opacity = "0";
        toast.style.transform = "translateY(-10px)";
        toast.style.transition = "0.3s ease";
        setTimeout(() => toast.remove(), 300);
    }, 3000 + index * 300);
}

window.showToast = showToast;
window.reloadTaskSubTasksPanel = reloadTaskSubTasksPanel;

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

async function reloadTaskSubTasksPanel(taskId, expandedSubTaskId) {
    if (!taskId) {
        return;
    }

    const detailsRow = document.querySelector(`.subtasks-row[data-task-details-id="${taskId}"]`);
    const currentPanel = detailsRow?.querySelector(".task-subtasks-panel");
    if (!detailsRow || !currentPanel) {
        return;
    }

    const expandedIds = Array.isArray(expandedSubTaskId)
        ? expandedSubTaskId.map(String)
        : expandedSubTaskId
            ? [String(expandedSubTaskId)]
            : Array.from(currentPanel.querySelectorAll(".subtask-entry.expanded"))
                .map(entry => entry.dataset.subtaskId)
                .filter(Boolean);

    const response = await fetch(`/MyTasks/SubTasksPanel?taskId=${encodeURIComponent(taskId)}`, {
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        }
    });

    if (!response.ok) {
        return;
    }

    currentPanel.outerHTML = await response.text();

    expandedIds.forEach(subTaskId => {
        const refreshedEntry = detailsRow.querySelector(`.subtask-entry[data-subtask-id="${subTaskId}"]`);
        if (refreshedEntry) {
            setSubTaskExpanded(refreshedEntry, true);
        }
    });
}

function updateTaskProgress(taskId, completionPercentage, projectedCompletionPercentage, completedSubTaskCount, totalSubTaskCount) {
    const progressContainer = document.querySelector(`.task-progress[data-task-progress-id="${taskId}"]`);
    if (!progressContainer) {
        return;
    }

    const progressFill = progressContainer.querySelector(".task-progress-fill-completed");
    const projectedProgressFill = progressContainer.querySelector(".task-progress-fill-projected");
    const progressText = progressContainer.querySelector(".task-progress-text");

    if (progressFill) {
        progressFill.style.width = `${completionPercentage}%`;
    }

    if (projectedProgressFill) {
        const projectedWidth = Math.max(projectedCompletionPercentage ?? completionPercentage, completionPercentage);
        projectedProgressFill.style.width = `${projectedWidth}%`;
    }

    if (progressText) {
        progressText.textContent = `${completionPercentage}% (${completedSubTaskCount}/${totalSubTaskCount})`;
    }
}

function reorderSubTaskEntries(taskId) {
    if (!taskId) {
        return;
    }

    const taskDetailsRow = document.querySelector(`.subtasks-row[data-task-details-id="${taskId}"]`);
    const subtaskList = taskDetailsRow?.querySelector(".subtask-list");
    if (!subtaskList) {
        return;
    }

    const subtaskEntries = Array.from(subtaskList.querySelectorAll(".subtask-entry"));
    const subtaskById = new Map(subtaskEntries.map(entry => [entry.dataset.subtaskId, entry]));
    const childrenByParentId = new Map();
    const rootEntries = [];
    const visited = new Set();

    subtaskEntries.forEach(entry => {
        const dependencyId = getDependencyValue(entry);
        if (dependencyId && subtaskById.has(dependencyId)) {
            if (!childrenByParentId.has(dependencyId)) {
                childrenByParentId.set(dependencyId, []);
            }

            childrenByParentId.get(dependencyId).push(entry);
        } else {
            rootEntries.push(entry);
        }
    });

    const orderedEntries = [];

    function appendEntry(entry, depth) {
        const subtaskId = entry.dataset.subtaskId;
        if (!subtaskId || visited.has(subtaskId)) {
            return;
        }

        visited.add(subtaskId);
        entry.style.setProperty("--subtask-depth", depth);
        entry.classList.toggle("has-dependency", depth > 0);
        orderedEntries.push(entry);

        const children = (childrenByParentId.get(subtaskId) ?? []).sort(compareSubTaskEntries);
        children.forEach(childEntry => appendEntry(childEntry, depth + 1));
    }

    rootEntries.sort(compareSubTaskEntries).forEach(entry => appendEntry(entry, 0));
    subtaskEntries.forEach(entry => appendEntry(entry, Number(entry.style.getPropertyValue("--subtask-depth")) || 0));
    orderedEntries.forEach(entry => subtaskList.appendChild(entry));
}

function compareSubTaskEntries(leftEntry, rightEntry) {
    return Number(leftEntry.dataset.subtaskId) - Number(rightEntry.dataset.subtaskId);
}

function syncDependencyOptions(selectElement, validDependencyIds) {
    if (!Array.isArray(validDependencyIds)) {
        return;
    }

    Array.from(selectElement.options).forEach(option => {
        if (!option.value) {
            option.hidden = false;
            option.disabled = false;
            return;
        }

        const isValid = validDependencyIds.includes(Number(option.value));
        option.hidden = !isValid;
        option.disabled = !isValid;
    });
}

function refreshDependencyOptions(taskId) {
    if (!taskId) {
        return;
    }

    const taskDetailsRow = document.querySelector(`.subtasks-row[data-task-details-id="${taskId}"]`);
    const subtaskEntries = Array.from(taskDetailsRow?.querySelectorAll(".subtask-entry") ?? []);
    const dependencyBySubTaskId = new Map(
        subtaskEntries.map(entry => [Number(entry.dataset.subtaskId), getDependencyValue(entry) ? Number(getDependencyValue(entry)) : null])
    );

    subtaskEntries.forEach(entry => {
        const currentSubTaskId = Number(entry.dataset.subtaskId);
        const selectElement = entry.querySelector(".subtask-inline-dependency-select");
        if (!selectElement) {
            return;
        }

        Array.from(selectElement.options).forEach(option => {
            if (!option.value) {
                option.hidden = false;
                option.disabled = false;
                return;
            }

            const candidateId = Number(option.value);
            const isValid = candidateId !== currentSubTaskId
                && !createsDependencyCycleClient(currentSubTaskId, candidateId, dependencyBySubTaskId);

            option.hidden = !isValid;
            option.disabled = !isValid;
        });
    });
}

function createsDependencyCycleClient(subTaskId, dependencyId, dependencyBySubTaskId) {
    let currentDependencyId = dependencyId;
    const visitedIds = new Set();

    while (currentDependencyId) {
        if (currentDependencyId === subTaskId || visitedIds.has(currentDependencyId)) {
            return true;
        }

        visitedIds.add(currentDependencyId);
        currentDependencyId = dependencyBySubTaskId.get(currentDependencyId) ?? null;
    }

    return false;
}
