const EMPTY_DESCRIPTION_TEXT = "No description";

//При зареждане на страницата се изпълява тази функция
document.addEventListener("DOMContentLoaded", function () {
    const taskRows = document.querySelectorAll(".task-summary-row");

    if (taskRows.length === 0) {
        return;
    }
    //Намират се всички редове .subtasks-row, които имат data-task-details-id
    //За всеки такъв ред се вика refreshDependencyOptions()
    document.querySelectorAll(".subtasks-row[data-task-details-id]").forEach(row => {
        refreshDependencyOptions(row.dataset.taskDetailsId);
    });

    //Глобален click listener
    document.addEventListener("click", function (event) {
        //Клик върху ред на задача и кликнатият елемент не е интерактирвен
        //Отваря или затваря детайлите на задачата
        const taskRow = event.target.closest(".task-summary-row");
        if (taskRow && !isInteractiveElement(event.target)) {
            toggleTaskDetails(taskRow);
            return;
        }

        //Клик върху бутон за редакция на dependency
        const dependencyTrigger = event.target.closest(".inline-dependency-trigger");
        if (dependencyTrigger) {
            event.preventDefault();
            //Спира bubbling-а на събитието нагоре.
            //За да не стигне кликът до .task-summary-row и случайно да отвори/затвори задачата.
            event.stopPropagation();
            const currentEntry = dependencyTrigger.closest(".subtask-entry");
            if (currentEntry) {
                openDependencyEditor(currentEntry);
            }
            return;
        }

        //Клик върху бутон за показване/скриване на описание на подзадача
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

        //Клик извън отворен dependency editor
        document.querySelectorAll(".subtask-entry.dependency-editing").forEach(entry => {
            if (!entry.contains(event.target)) {
                closeDependencyEditor(entry);
            }
        });
    });

    //Временно запазване на стойността на текста в редактиране
    document.addEventListener("focusin", function (event) {
        const editableField = event.target.closest(".inline-editable");
        if (!editableField) {
            return;
        }

        editableField.dataset.originalValue = normalizeEditableValue(editableField);
    });

    //Слуша за натискане на клавиши
    document.addEventListener("keydown", function (event) {
        const editableField = event.target.closest(".inline-editable");
        if (!editableField) {
            return;
        }

        //"Задава текста към новата стойност"
        if (event.key === "Enter" && editableField.dataset.inlineField === "title") {
            event.preventDefault();
            editableField.blur();
        }

        //Връща текста към оригиналната стойност
        if (event.key === "Escape") {
            event.preventDefault();
            editableField.textContent = editableField.dataset.originalValue ?? editableField.textContent ?? "";
            editableField.blur();
        }
    });

    //Когато поле изгуби фокус
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

    //Listener за смяна на зависимост между подзадачите
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

    //Следи кога dependency select губи фокус.
    document.addEventListener("focusout", function (event) {
        const dependencySelect = event.target.closest(".subtask-inline-dependency-select");
        if (!dependencySelect) {
            return;
        }

        const subtaskEntry = dependencySelect.closest(".subtask-entry");
        if (!subtaskEntry) {
            return;
        }

        //Изчаква browser-а да премести focus-а, за да провери дали editor-ът е напуснат
        window.setTimeout(() => {
            if (!subtaskEntry.contains(document.activeElement)) {
                closeDependencyEditor(subtaskEntry);
            }
        }, 0);
    });

    //Submit listener за форми
    document.addEventListener("submit", function (event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        //Форма за смяна на статус
        if (form.matches(".subtask-cycle-form")) {
            event.preventDefault();
            cycleSubTaskStatus(form);
            return;
        }

        //Форма за добавяне на подзадача
        if (form.matches(".subtask-add-form")) {
            event.preventDefault();
            addSubTask(form);
            return;
        }

        //Форма за триене
        if (form.matches(".subtask-delete-form")) {
            event.preventDefault();
            deleteSubTask(form);
        }
    });
});

//Превключва отворено или затворено състояние на реда с подзадачите
function toggleTaskDetails(taskRow) {
    //Ако редът е в режим редакция, не трябва да се сгъва/разгъва.
    if (taskRow.classList.contains("editing")) {
        return;
    }

    const taskId = taskRow.dataset.taskRowId;
    const detailsRow = document.querySelector(`.subtasks-row[data-task-details-id="${taskId}"]`);

    if (!detailsRow) {
        return;
    }

    const shouldOpen = !detailsRow.classList.contains("open");

    //Затвори всички други задачи
    document.querySelectorAll(".task-summary-row.expanded").forEach(row => {
        if (row !== taskRow) {
            row.classList.remove("expanded");
        }
    });

    //Затвори всички други detail rows
    document.querySelectorAll(".subtasks-row.open").forEach(row => {
        if (row !== detailsRow) {
            row.classList.remove("open");
        }
    });

    taskRow.classList.toggle("expanded", shouldOpen);
    detailsRow.classList.toggle("open", shouldOpen);
}

//Излиза от edit mode на подзадача
function exitSubtaskEditMode(subtaskEntry) {
    closeDependencyEditor(subtaskEntry);
}

//Сменя статуса на подзадача циклично чрез AJAX
async function cycleSubTaskStatus(form) {
    //Изчаква AJAX заявката да приключи.
    const result = await postForm(form);
    if (!result) {
        return;
    }

    if (!result.success) {
        showToast(result.message, result.notificationCssClass);
        return;
    }

    //Обновява прогрес бара
    updateTaskProgress(
        result.taskId,
        result.completionPercentage,
        result.projectedCompletionPercentage,
        result.completedSubTaskCount,
        result.totalSubTaskCount);
    //Презарежда HTML панела с подзадачите
    await reloadTaskSubTasksPanel(result.taskId);
    //Показва toast
    showToast(result.message, result.notificationCssClass);
}

//Добавя нова подзадача към избраната главна задача
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

//Изтрива подзадача и обновява панела след успех
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

//Проверява дали елементът е интерактивен и не трябва да отваря реда
function isInteractiveElement(element) {
    return !!element.closest("button, a, form, input, select, textarea, label, .popup-actions, .row-actions, .subtask-actions, .subtask-edit-form, .inline-editable, .inline-dependency-trigger");
}

//Записва inline редакция на подзадача чрез hidden form-а
async function saveSubTaskChanges(subtaskEntry, payload) {
    if (subtaskEntry.dataset.isSaving === "true") {
        return;
    }

    subtaskEntry.dataset.isSaving = "true";

    //Търси формата, която ще се използва за submit.
    const editForm = subtaskEntry.querySelector(".subtask-edit-form");
    if (!(editForm instanceof HTMLFormElement)) {
        subtaskEntry.dataset.isSaving = "false";
        return;
    }

    //Взима всички полета от формата и после им заменя стойностите с актуалните.
    const formData = new FormData(editForm);
    formData.set("Title", payload.title);
    formData.set("Description", payload.description);
    formData.set("BlockedBySubTaskId", payload.blockedBySubTaskId ?? "");

    //AJAX заявка
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

//Отваря select-а за редакция на dependency
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

//Затваря dependency editor-а и възстановява текущата стойност
function closeDependencyEditor(subtaskEntry, selectedValue) {
    subtaskEntry.classList.remove("dependency-editing");

    const dependencySelect = subtaskEntry.querySelector(".subtask-inline-dependency-select");
    const hiddenDependency = subtaskEntry.querySelector('input[name="BlockedBySubTaskId"]');
    if (dependencySelect) {
        dependencySelect.value = selectedValue ?? hiddenDependency?.value ?? "";
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

//Създава fallback toast-container и показва нотификация
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

//Обновява визуално прогреса на главната задача
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

//Подрежда подзадачите в стълбовиден вид според зависимостите им
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

    //Обхожда дървото depth-first, за да постави децата под техния родител
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

//Сравнява подзадачите по id за стабилен ред на рендериране
function compareSubTaskEntries(leftEntry, rightEntry) {
    return Number(leftEntry.dataset.subtaskId) - Number(rightEntry.dataset.subtaskId);
}
