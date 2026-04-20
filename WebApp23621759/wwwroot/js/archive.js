document.addEventListener("DOMContentLoaded", function () {
    document.addEventListener("click", function (event) {
        const descriptionToggle = event.target.closest("[data-archive-description-toggle]");
        if (descriptionToggle) {
            event.preventDefault();
            event.stopPropagation();

            const wrapper = descriptionToggle.closest(".archive-description-box");
            const isOpen = wrapper?.classList.toggle("description-open");
            descriptionToggle.setAttribute("aria-expanded", String(!!isOpen));
            return;
        }

        const subTaskEntry = event.target.closest(".archive-subtask-entry");
        if (subTaskEntry && !isArchiveInteractiveElement(event.target)) {
            event.preventDefault();
            event.stopPropagation();
            subTaskEntry.classList.toggle("expanded");
            return;
        }

        const taskRow = event.target.closest(".archive-task-row");
        if (taskRow && !isArchiveInteractiveElement(event.target)) {
            toggleArchiveDetails(taskRow);
        }
    });

    document.addEventListener("submit", async function (event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.matches(".archive-action-form")) {
            return;
        }

        event.preventDefault();

        const result = await postForm(form);
        if (!result) {
            return;
        }

        if (typeof hideAllTaskPopups === "function") {
            hideAllTaskPopups();
        }

        showToast(result.message, result.notificationCssClass);
        if (result.success) {
            document.querySelector(`[data-archive-task-id="${result.taskId}"]`)?.remove();
            document.querySelector(`[data-archive-details-id="${result.taskId}"]`)?.remove();
            syncArchiveEmptyState();
        }
    });
});

//Проверява дали кликът е върху бутон или форма, за да не отваря детайлите на реда
function isArchiveInteractiveElement(element) {
    return !!element.closest("button, a, form, input, select, textarea, label, .row-actions, .delete-wrapper, .done-wrapper");
}

//Отваря или скрива readonly панела с описанието и подзадачите на архивираната задача
function toggleArchiveDetails(taskRow) {
    const taskId = taskRow.dataset.archiveTaskId;
    const detailsRow = document.querySelector(`[data-archive-details-id="${taskId}"]`);
    const isOpen = detailsRow?.classList.toggle("open");
    taskRow.classList.toggle("expanded", !!isOpen);
}

//Показва празно състояние, когато последната архивирана задача бъде върната или изтрита
function syncArchiveEmptyState() {
    if (document.querySelector(".archive-task-row")) {
        return;
    }

    const wrapper = document.querySelector(".archive-card .table-wrapper");
    if (wrapper) {
        wrapper.outerHTML = `<div class="archive-empty">No archived tasks yet.</div>`;
    }
}
