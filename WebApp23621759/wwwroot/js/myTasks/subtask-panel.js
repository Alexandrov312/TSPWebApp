//Превключва отворено или затворено състояние на реда с подзадачите
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

    //Държи отворен само един главен ред с подзадачи в даден момент
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

//Презарежда само HTML панела с подзадачите за конкретна задача
async function reloadTaskSubTasksPanel(taskId, expandedSubTaskId) {
    if (!taskId) {
        return;
    }

    const currentPanels = Array.from(document.querySelectorAll(`.task-subtasks-panel[data-task-panel-id="${taskId}"]`));
    if (!currentPanels.length) {
        return;
    }

    const endpoint = currentPanels[0].dataset.subtasksEndpoint;
    if (!endpoint) {
        return;
    }

    const expandedIdsByPanel = new Map(currentPanels.map(panel => {
        const expandedIds = Array.isArray(expandedSubTaskId)
            ? expandedSubTaskId.map(String)
            : expandedSubTaskId
                ? [String(expandedSubTaskId)]
                : Array.from(panel.querySelectorAll(".subtask-entry.expanded"))
                    .map(entry => entry.dataset.subtaskId)
                    .filter(Boolean);

        return [panel, expandedIds];
    }));

    const response = await fetch(endpoint, {
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        }
    });

    if (!response.ok) {
        return;
    }

    const panelHtml = await response.text();

    currentPanels.forEach(panel => {
        panel.outerHTML = panelHtml;
    });

    //След всеки refresh преизчислява валидните dependency опции
    refreshDependencyOptions(taskId);

    const refreshedPanels = Array.from(document.querySelectorAll(`.task-subtasks-panel[data-task-panel-id="${taskId}"]`));
    refreshedPanels.forEach((refreshedPanel, index) => {
        const previousPanel = currentPanels[index];
        const expandedIds = expandedIdsByPanel.get(previousPanel) ?? [];
        expandedIds.forEach(subTaskId => {
            const refreshedEntry = refreshedPanel.querySelector(`.subtask-entry[data-subtask-id="${subTaskId}"]`);
            if (refreshedEntry) {
                setSubTaskExpanded(refreshedEntry, true);
            }
        });
    });
}

//Задава дали описанието на подзадача е разгънато
function setSubTaskExpanded(subtaskEntry, isExpanded) {
    subtaskEntry.classList.toggle("expanded", isExpanded);

    const toggleButton = subtaskEntry.querySelector("[data-subtask-toggle]");
    if (toggleButton) {
        toggleButton.setAttribute("aria-expanded", String(isExpanded));
        toggleButton.setAttribute("title", isExpanded ? "Hide description" : "Show description");
    }
}

//Показва или скрива описанието на конкретна подзадача
function toggleSubTaskDescription(subtaskEntry) {
    const shouldExpand = !subtaskEntry.classList.contains("expanded");
    setSubTaskExpanded(subtaskEntry, shouldExpand);
}

//Синхронизира кои dependency options са валидни в даден select
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

//Пресмята dependency опциите на всяка подзадача спрямо текущото състояние в UI
function refreshDependencyOptions(taskId) {
    if (!taskId) {
        return;
    }

    const currentPanel = document.querySelector(`.task-subtasks-panel[data-task-panel-id="${taskId}"]`);
    const subtaskEntries = Array.from(currentPanel?.querySelectorAll(".subtask-entry") ?? []);
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

//Проверява дали нова зависимост би създала цикъл в клиента
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

window.toggleTaskDetails = toggleTaskDetails;
window.reloadTaskSubTasksPanel = reloadTaskSubTasksPanel;
window.setSubTaskExpanded = setSubTaskExpanded;
window.toggleSubTaskDescription = toggleSubTaskDescription;
window.syncDependencyOptions = syncDependencyOptions;
window.refreshDependencyOptions = refreshDependencyOptions;
