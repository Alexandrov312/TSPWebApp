let myTasksDraggedKanbanCard = null;
let myTasksDraggedFromDropzone = null;
let myTasksDraggedNextSibling = null;

document.addEventListener("DOMContentLoaded", function () {
    const query = new URLSearchParams(window.location.search);
    const initialTaskId = query.get("kanbanTaskId") || window.initialKanbanTaskId;
    if (initialTaskId) {
        const renderedBoard = document.querySelector(`.mytasks-kanban-board[data-kanban-task-id="${initialTaskId}"]`);
        if (renderedBoard) {
            document.querySelector(".mytasks-list-view")?.classList.add("is-hidden");
            document.querySelector(".mytasks-kanban-view")?.classList.remove("is-hidden");
        } else {
            openMyTasksKanban(
                initialTaskId,
                false,
                query.get("source") || window.initialKanbanSource || "mytasks",
                query.get("returnUrl") || window.initialKanbanReturnUrl || "");
        }
    }

    document.addEventListener("click", function (event) {
        const openButton = event.target.closest(".open-kanban-btn");
        if (openButton) {
            event.preventDefault();
            event.stopPropagation();
            openMyTasksKanban(openButton.dataset.kanbanTaskId, true, openButton.dataset.kanbanSource || "mytasks", "");
            return;
        }

        const backButton = event.target.closest(".kanban-back-btn");
        if (backButton) {
            event.preventDefault();
            goBackFromMyTasksKanban();
            return;
        }

        const descriptionToggle = event.target.closest("[data-kanban-description-toggle]");
        if (descriptionToggle) {
            event.preventDefault();
            const board = descriptionToggle.closest(".mytasks-kanban-board");
            const isExpanded = board?.classList.toggle("description-open");
            descriptionToggle.setAttribute("aria-expanded", String(!!isExpanded));
            return;
        }

        const dependencyTrigger = event.target.closest(".kanban-dependency-trigger");
        if (dependencyTrigger) {
            event.preventDefault();
            event.stopPropagation();
            if (isKanbanDependencyTriggerLocked(dependencyTrigger)) {
                return;
            }
            const card = dependencyTrigger.closest(".mytasks-kanban-card");
            if (card) {
                openKanbanDependencyEditor(card);
            }
            return;
        }

        const detailButton = event.target.closest("[data-kanban-card-details]");
        if (detailButton) {
            event.preventDefault();
            event.stopPropagation();
            const card = detailButton.closest(".mytasks-kanban-card");
            if (card) {
                toggleKanbanCardDetails(card);
            }
            return;
        }

        const closeDetailsButton = event.target.closest(".kanban-card-detail-close");
        if (closeDetailsButton) {
            event.preventDefault();
            event.stopPropagation();
            closeKanbanCardDetails(closeDetailsButton.closest(".mytasks-kanban-card"));
            return;
        }

        const card = event.target.closest(".mytasks-kanban-card");
        if (card && !isKanbanCardInteractiveElement(event.target)) {
            toggleKanbanCardDetails(card);
            return;
        }

        document.querySelectorAll(".mytasks-kanban-card.dependency-editing").forEach(entry => {
            if (!entry.contains(event.target)) {
                closeKanbanDependencyEditor(entry);
            }
        });
    });

    document.addEventListener("submit", function (event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        if (form.matches(".task-create-form")) {
            event.preventDefault();
            createMyTasksTask(form);
            return;
        }

        if (form.matches(".kanban-subtask-add-form")) {
            event.preventDefault();
            addMyTasksKanbanSubTask(form);
            return;
        }

        if (form.matches(".kanban-subtask-delete-form")) {
            event.preventDefault();
            deleteMyTasksKanbanSubTask(form);
            return;
        }

        if (form.matches(".kanban-move-form")) {
            event.preventDefault();
            moveMyTasksKanbanFromForm(form);
        }
    });

    document.addEventListener("change", function (event) {
        const dependencySelect = event.target.closest(".kanban-dependency-select");
        if (!dependencySelect) {
            return;
        }

        const card = dependencySelect.closest(".mytasks-kanban-card");
        if (card) {
            closeKanbanDependencyEditor(card, dependencySelect.value);
            saveMyTasksKanbanSubTask(card);
        }
    });

    document.addEventListener("focusin", function (event) {
        const editable = event.target.closest(".kanban-inline-title, .kanban-card-description-editable, .kanban-task-title-editable, .kanban-task-description-editable");
        if (editable) {
            editable.dataset.originalValue = normalizeEditableValue(editable);
        }
    });

    document.addEventListener("keydown", function (event) {
        const editable = event.target.closest(".kanban-inline-title, .kanban-card-description-editable, .kanban-task-title-editable, .kanban-task-description-editable");
        if (!editable) {
            return;
        }

        if (event.key === "Enter") {
            event.preventDefault();
            editable.blur();
        }

        if (event.key === "Escape") {
            event.preventDefault();
            editable.textContent = editable.dataset.originalValue ?? editable.textContent ?? "";
            editable.blur();
        }
    });

    document.addEventListener("focusout", function (event) {
        const editable = event.target.closest(".kanban-inline-title, .kanban-card-description-editable, .kanban-task-title-editable, .kanban-task-description-editable");
        if (!editable) {
            return;
        }

        const originalValue = editable.dataset.originalValue ?? "";
        const currentValue = normalizeEditableValue(editable);
        if (currentValue === originalValue) {
            return;
        }

        const card = editable.closest(".mytasks-kanban-card");
        if (card) {
            saveMyTasksKanbanSubTask(card);
            return;
        }

        const board = editable.closest(".mytasks-kanban-board");
        if (board) {
            saveMyTasksKanbanTask(board);
        }
    });

    document.addEventListener("dragstart", function (event) {
        const card = event.target.closest(".mytasks-kanban-card");
        if (!card) {
            return;
        }

        myTasksDraggedKanbanCard = card;
        myTasksDraggedFromDropzone = card.parentElement;
        myTasksDraggedNextSibling = card.nextElementSibling;
        card.classList.add("dragging");
        markMyTasksKanbanDropZones(card);
        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData("text/plain", card.dataset.subtaskId);
    });

    document.addEventListener("dragend", function () {
        clearMyTasksKanbanDropZones();
        if (myTasksDraggedKanbanCard) {
            myTasksDraggedKanbanCard.classList.remove("dragging");
        }
    });

    document.addEventListener("dragover", function (event) {
        const column = event.target.closest(".mytasks-kanban-column");
        if (!column || !myTasksDraggedKanbanCard || !column.classList.contains("drop-valid")) {
            return;
        }

        event.preventDefault();
        column.classList.add("drag-over");
    });

    document.addEventListener("dragleave", function (event) {
        const column = event.target.closest(".mytasks-kanban-column");
        if (column && !column.contains(event.relatedTarget)) {
            column.classList.remove("drag-over");
        }
    });

    document.addEventListener("drop", function (event) {
        const column = event.target.closest(".mytasks-kanban-column");
        if (!column || !myTasksDraggedKanbanCard || !column.classList.contains("drop-valid")) {
            return;
        }

        event.preventDefault();
        moveMyTasksKanbanCard(column);
    });

    window.addEventListener("popstate", function () {
        const query = new URLSearchParams(window.location.search);
        const taskId = query.get("kanbanTaskId");
        if (taskId) {
            openMyTasksKanban(taskId, false, query.get("source") || "mytasks", query.get("returnUrl") || "");
        } else {
            showMyTasksListView();
        }
    });
});

//Зарежда Kanban partial-а за избраната главна задача
async function openMyTasksKanban(taskId, shouldPushState = true, source = "mytasks", returnUrl = "") {
    const kanbanView = document.querySelector(".mytasks-kanban-view");
    const listView = document.querySelector(".mytasks-list-view");
    if (!kanbanView || !listView || !taskId) {
        return;
    }

    const panelQuery = new URLSearchParams({ taskId, source });
    if (returnUrl) {
        panelQuery.set("returnUrl", returnUrl);
    }

    const response = await fetch(`/MyTasks/KanbanPanel?${panelQuery.toString()}`, {
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        }
    });

    if (!response.ok) {
        showToast("Kanban view could not be loaded.", "toast-error");
        return;
    }

    kanbanView.innerHTML = await response.text();
    listView.classList.add("is-hidden");
    kanbanView.classList.remove("is-hidden");

    if (shouldPushState) {
        const routeQuery = new URLSearchParams({ kanbanTaskId: taskId, source });
        if (returnUrl) {
            routeQuery.set("returnUrl", returnUrl);
        }

        history.pushState({ mode: "kanban", taskId, source, returnUrl }, "", `/MyTasks?${routeQuery.toString()}`);
    }
}

//Връща потребителя според мястото, от което е отворен Kanban режимът
function goBackFromMyTasksKanban() {
    const board = document.querySelector(".mytasks-kanban-board");
    const source = board?.dataset.kanbanSource || "mytasks";
    const returnUrl = board?.dataset.kanbanReturnUrl || "";

    if (source === "calendar" && returnUrl) {
        window.location.href = returnUrl;
        return;
    }

    showMyTasksListView();
    history.pushState({ mode: "table" }, "", "/MyTasks");
}

//Връща стандартната таблица с главни задачи
function showMyTasksListView() {
    document.querySelector(".mytasks-kanban-view")?.classList.add("is-hidden");
    document.querySelector(".mytasks-list-view")?.classList.remove("is-hidden");
}

//Създава нова главна задача и обновява таблицата без full reload
async function createMyTasksTask(form) {
    const result = await postForm(form);
    if (!result) {
        return;
    }

    showToast(result.message, result.notificationCssClass);
    if (result.success) {
        await refreshMyTasksListView(result.taskId);
    }
}

//Добавя подзадача от Kanban режима
async function addMyTasksKanbanSubTask(form) {
    if (form.dataset.isSubmitting === "true") {
        return;
    }

    form.dataset.isSubmitting = "true";
    const result = await postForm(form);
    form.dataset.isSubmitting = "false";
    await handleMyTasksKanbanMutation(result);
}

//Изтрива подзадача от Kanban режима
async function deleteMyTasksKanbanSubTask(form) {
    if (typeof hideAllTaskPopups === "function") {
        hideAllTaskPopups();
    }

    const result = await postForm(form);
    await handleMyTasksKanbanMutation(result);
}

//Премества подзадача чрез компактен action бутон
async function moveMyTasksKanbanFromForm(form) {
    const result = await postForm(form);
    await handleMyTasksKanbanMutation(result);
}

//Записва inline редакция на главната задача в Kanban режима
async function saveMyTasksKanbanTask(board) {
    const form = board.querySelector(".kanban-task-update-form");
    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    const title = normalizeEditableValue(board.querySelector(".kanban-task-title-editable"));
    const description = normalizeEditableValue(board.querySelector(".kanban-task-description-editable"));
    if (!title) {
        showToast("Task title is required.", "toast-error");
        await reloadMyTasksKanbanBoard(board.dataset.kanbanTaskId);
        return;
    }

    const formData = new FormData(form);
    formData.set("Title", title);
    formData.set("Description", description || "Task description");

    const result = await postForm(form, formData);
    if (!result) {
        return;
    }

    showToast(result.message, result.notificationCssClass);
    if (result.success) {
        await refreshMyTasksListView();
        await reloadMyTasksKanbanBoard(result.taskId);
    }
}

//Записва inline промяна по подзадача от Kanban card
async function saveMyTasksKanbanSubTask(card) {
    const title = normalizeEditableValue(card.querySelector(".kanban-inline-title"));
    if (!title) {
        showToast("Subtask title is required.", "toast-error");
        await reloadMyTasksKanbanBoard(card.dataset.taskId);
        return;
    }

    const form = card.querySelector(".subtask-edit-form");
    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    const formData = new FormData(form);
    formData.set("Title", title);
    formData.set("Description", getKanbanCardDescriptionValue(card));
    formData.set("BlockedBySubTaskId", card.querySelector(".kanban-dependency-select")?.value ?? "");

    const result = await postForm(form, formData);
    await handleMyTasksKanbanMutation(result);
}

//Обработва общия JSON отговор след add/edit/delete/move в Kanban режима
async function handleMyTasksKanbanMutation(result) {
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

    await reloadMyTasksKanbanBoard(result.taskId);
    if (typeof reloadTaskSubTasksPanel === "function") {
        await reloadTaskSubTasksPanel(result.taskId);
    }
}

//Презарежда стандартната таблица през AJAX без full page reload
async function refreshMyTasksListView(scrollToTaskId) {
    const query = new URLSearchParams(window.location.search);
    query.delete("kanbanTaskId");
    query.delete("source");
    query.delete("returnUrl");
    const endpoint = query.toString() ? `/MyTasks?${query.toString()}` : "/MyTasks";

    const response = await fetch(endpoint, {
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        }
    });

    if (!response.ok) {
        return;
    }

    const html = await response.text();
    const doc = new DOMParser().parseFromString(html, "text/html");
    const refreshedList = doc.querySelector(".mytasks-list-view");
    const currentList = document.querySelector(".mytasks-list-view");
    if (refreshedList && currentList) {
        currentList.innerHTML = refreshedList.innerHTML;
        if (typeof applyPersistedViewModes === "function") {
            applyPersistedViewModes();
        }

        if (scrollToTaskId) {
            scrollToCreatedTask(scrollToTaskId);
        }
    }
}

//Скролва до новосъздадената задача след AJAX refresh на MyTasks
function scrollToCreatedTask(taskId) {
    window.requestAnimationFrame(() => {
        const isGridView = document.documentElement.dataset.mytasksView === "grid";
        const primarySelector = isGridView
            ? `.task-grid-card[data-task-grid-id="${taskId}"]`
            : `.task-summary-row[data-task-row-id="${taskId}"]`;
        const fallbackSelector = isGridView
            ? `.task-summary-row[data-task-row-id="${taskId}"]`
            : `.task-grid-card[data-task-grid-id="${taskId}"]`;
        const target = document.querySelector(primarySelector) ?? document.querySelector(fallbackSelector);
        if (!target) {
            return;
        }

        target.scrollIntoView({ behavior: "smooth", block: "center" });
        target.classList.add("created-task-highlight");
        window.setTimeout(() => target.classList.remove("created-task-highlight"), 1400);
    });
}

//Маркира само позволените drop зони според dependency ограниченията
function markMyTasksKanbanDropZones(card) {
    const isBlocked = card.dataset.isBlocked === "true";

    document.querySelectorAll(".mytasks-kanban-column").forEach(column => {
        const status = Number(column.dataset.kanbanStatus);
        const isValid = !isBlocked || status === 0;

        column.classList.toggle("drop-valid", isValid);
        column.classList.toggle("drop-invalid", !isValid);
    });
}

//Изчиства визуалното drag-and-drop състояние
function clearMyTasksKanbanDropZones() {
    document.querySelectorAll(".mytasks-kanban-column")
        .forEach(column => column.classList.remove("drop-valid", "drop-invalid", "drag-over"));
}

//Премества card-а оптимистично и връща назад, ако backend-ът откаже
async function moveMyTasksKanbanCard(targetColumn) {
    const targetDropzone = targetColumn.querySelector(".mytasks-kanban-dropzone");
    if (!targetDropzone || !myTasksDraggedKanbanCard) {
        return;
    }

    const card = myTasksDraggedKanbanCard;
    const oldDropzone = myTasksDraggedFromDropzone;
    const oldNextSibling = myTasksDraggedNextSibling;
    const oldStatus = card.dataset.currentStatus;
    const targetStatus = targetColumn.dataset.kanbanStatus;

    targetDropzone.appendChild(card);
    card.dataset.currentStatus = targetStatus;

    const formData = new FormData();
    formData.set("id", card.dataset.subtaskId);
    formData.set("targetStatus", targetStatus);

    const response = await fetch(getMyTasksKanbanMoveEndpoint(), {
        method: "POST",
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        body: formData
    });

    if (!response.ok) {
        rollbackMyTasksKanbanMove(card, oldDropzone, oldNextSibling, oldStatus);
        showToast("Status update failed.", "toast-error");
        return;
    }

    const result = await response.json();
    if (!result.success) {
        rollbackMyTasksKanbanMove(card, oldDropzone, oldNextSibling, oldStatus);
        showToast(result.message, result.notificationCssClass);
        return;
    }

    await handleMyTasksKanbanMutation(result);
}

//Връща card-а на старото място при неуспешен drop
function rollbackMyTasksKanbanMove(card, oldDropzone, oldNextSibling, oldStatus) {
    if (!oldDropzone) {
        return;
    }

    if (oldNextSibling && oldNextSibling.parentElement === oldDropzone) {
        oldDropzone.insertBefore(card, oldNextSibling);
    } else {
        oldDropzone.appendChild(card);
    }

    card.dataset.currentStatus = oldStatus;
}

//Презарежда само Kanban board-а за текущата главна задача
async function reloadMyTasksKanbanBoard(taskId) {
    const kanbanView = document.querySelector(".mytasks-kanban-view");
    if (!kanbanView || !taskId || kanbanView.classList.contains("is-hidden")) {
        return;
    }

    const board = document.querySelector(".mytasks-kanban-board");
    const source = board?.dataset.kanbanSource || "mytasks";
    const returnUrl = board?.dataset.kanbanReturnUrl || "";
    const panelQuery = new URLSearchParams({ taskId, source });
    if (returnUrl) {
        panelQuery.set("returnUrl", returnUrl);
    }

    const response = await fetch(`/MyTasks/KanbanPanel?${panelQuery.toString()}`, {
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        }
    });

    if (response.ok) {
        kanbanView.innerHTML = await response.text();
    }
}

//Взима endpoint-а за drag-and-drop move
function getMyTasksKanbanMoveEndpoint() {
    return document.querySelector(".mytasks-kanban-board")?.dataset.kanbanMoveEndpoint ?? "/MyTasks/MoveSubTaskStatus";
}

//Отваря dependency selector-а върху самия badge
function openKanbanDependencyEditor(card) {
    const dependencyTrigger = card.querySelector(".kanban-dependency-trigger");
    if (isKanbanDependencyTriggerLocked(dependencyTrigger)) {
        return;
    }

    document.querySelectorAll(".mytasks-kanban-card.dependency-editing").forEach(entry => {
        if (entry !== card) {
            closeKanbanDependencyEditor(entry);
        }
    });

    card.classList.add("dependency-editing");
    const select = card.querySelector(".kanban-dependency-select");
    if (select) {
        select.focus();
        select.click();
    }
}

//Затваря dependency editor-а и пази избраната стойност във формата
function closeKanbanDependencyEditor(card, selectedValue) {
    if (!card) {
        return;
    }

    card.classList.remove("dependency-editing");
    const select = card.querySelector(".kanban-dependency-select");
    const hiddenDependency = card.querySelector('input[name="BlockedBySubTaskId"]');
    if (select) {
        select.value = selectedValue ?? hiddenDependency?.value ?? "";
    }
    if (hiddenDependency && selectedValue !== undefined) {
        hiddenDependency.value = selectedValue ?? "";
    }
}

//Показва или скрива компактния popover с описание на подзадача
function toggleKanbanCardDetails(card) {
    const shouldOpen = !card.classList.contains("details-open");
    document.querySelectorAll(".mytasks-kanban-card.details-open").forEach(openCard => {
        if (openCard !== card) {
            closeKanbanCardDetails(openCard);
        }
    });

    card.classList.toggle("details-open", shouldOpen);
}

//Затваря popover-а с описание
function closeKanbanCardDetails(card) {
    card?.classList.remove("details-open");
}

//Взима описанието на card-а и маха placeholder текста
function getKanbanCardDescriptionValue(card) {
    const description = normalizeEditableValue(card.querySelector(".kanban-card-description-editable"));
    return description === EMPTY_DESCRIPTION_TEXT ? "" : description;
}

//Пази card click-а от конфликт с бутони, popups и inline edit
function isKanbanCardInteractiveElement(element) {
    return !!element.closest("button, form, input, select, textarea, label, .delete-wrapper, .popup-actions, .kanban-inline-title, .kanban-card-description-editable, .kanban-dependency-select, .kanban-dependency-trigger");
}

//Проверява дали dependency badge-ът е заключен, защото подзадачата е завършена
function isKanbanDependencyTriggerLocked(dependencyTrigger) {
    return dependencyTrigger?.disabled || dependencyTrigger?.classList.contains("dependency-locked");
}
