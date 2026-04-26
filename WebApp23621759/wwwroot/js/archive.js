document.addEventListener("DOMContentLoaded", function () {
    //Capture listener-ът хваща Yes бутона в restore popup-а, дори когато popup-ът е преместен в body.
    document.addEventListener("click", function (event) {
        const restoreConfirmButton = event.target.closest("[data-restore-confirm]");
        if (restoreConfirmButton) {
            event.preventDefault();
            event.stopImmediatePropagation();

            const form = getArchivePopupForm(restoreConfirmButton);
            if (form) {
                form.dataset.restoreConfirmed = "true";
                form.requestSubmit();
            }
        }
    }, true);

    //Основен click listener за readonly details panel-и в Archive.
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
            return;
        }

        const archiveCard = event.target.closest(".archive-grid-card");
        if (archiveCard && !isArchiveInteractiveElement(event.target)) {
            toggleArchiveCardDetails(archiveCard.dataset.archiveCardId);
        }
    });

    //Restore/Delete действията в Archive са AJAX, за да се маха задачата без презареждане.
    document.addEventListener("submit", async function (event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.matches(".archive-action-form")) {
            return;
        }

        event.preventDefault();

        if (form.action.includes("/Restore") && form.dataset.restoreConfirmed !== "true") {
            const wrapper = form.closest(".restore-wrapper");
            const popup = form.querySelector(".restore-popup");
            if (typeof hideAllTaskPopups === "function") {
                hideAllTaskPopups();
            }
            if (typeof openTaskPopup === "function" && wrapper && popup) {
                openTaskPopup(wrapper, popup);
            }
            return;
        }

        delete form.dataset.restoreConfirmed;

        const result = await postForm(form);
        if (!result) {
            return;
        }

        if (typeof hideAllTaskPopups === "function") {
            hideAllTaskPopups();
        }

        showToast(result.message, result.notificationCssClass);
        if (result.success) {
            document.querySelectorAll(`[data-archive-task-id="${result.taskId}"], [data-archive-details-id="${result.taskId}"], [data-archive-card-id="${result.taskId}"], [data-archive-card-details-id="${result.taskId}"]`)
                .forEach(element => element.remove());
            syncArchiveEmptyState();
        }
    });
});

//Проверява дали кликът е върху бутон или форма, за да не отваря детайлите на реда
function isArchiveInteractiveElement(element) {
    return !!element.closest("button, a, form, input, select, textarea, label, .row-actions, .delete-wrapper, .done-wrapper");
}

function getArchivePopupForm(button) {
    if (button.form) {
        return button.form;
    }

    const popup = button.closest(".done-popup, .delete-popup");
    const popupId = popup?.dataset.popupPortalId;
    if (!popupId) {
        return button.closest("form");
    }

    return document.querySelector(`.restore-wrapper[data-popup-portal-id="${popupId}"] form`);
}

//Отваря или скрива readonly панела с описанието и подзадачите на архивираната задача
function toggleArchiveDetails(taskRow) {
    const taskId = taskRow.dataset.archiveTaskId;
    const detailsRow = document.querySelector(`[data-archive-details-id="${taskId}"]`);
    const isOpen = detailsRow?.classList.toggle("open");
    taskRow.classList.toggle("expanded", !!isOpen);
}

//Отваря или скрива детайлите на архивирана задача в grid режима
function toggleArchiveCardDetails(taskId) {
    const card = document.querySelector(`[data-archive-card-id="${taskId}"]`);
    const details = document.querySelector(`[data-archive-card-details-id="${taskId}"]`);
    if (!card || !details) {
        return;
    }

    const shouldOpen = !details.classList.contains("open");
    document.querySelectorAll(".archive-grid-card.expanded").forEach(openCard => {
        if (openCard !== card) {
            openCard.classList.remove("expanded");
        }
    });

    document.querySelectorAll(".archive-grid-details.open").forEach(openDetails => {
        if (openDetails !== details) {
            openDetails.classList.remove("open");
        }
    });

    card.classList.toggle("expanded", shouldOpen);
    if (shouldOpen) {
        moveArchiveGridDetailsAfterCurrentRow(card, details);
    }

    details.classList.toggle("open", shouldOpen);
}

//Премества details панела след последната архивирана карта от същия ред
function moveArchiveGridDetailsAfterCurrentRow(card, details) {
    const grid = card.parentElement;
    if (!grid) {
        return;
    }

    const rowTop = card.offsetTop;
    const rowCards = Array.from(grid.querySelectorAll(".archive-grid-card"))
        .filter(candidate => candidate.offsetTop === rowTop);
    const lastCardInRow = rowCards[rowCards.length - 1] ?? card;
    lastCardInRow.insertAdjacentElement("afterend", details);
}

//Показва празно състояние, когато последната архивирана задача бъде върната или изтрита
function syncArchiveEmptyState() {
    if (document.querySelector(".archive-task-row, .archive-grid-card")) {
        return;
    }

    const card = document.querySelector(".archive-card");
    if (card) {
        card.querySelectorAll(".archive-view-mode").forEach(element => element.remove());
        if (!card.querySelector(".archive-empty")) {
            card.insertAdjacentHTML("beforeend", `<div class="archive-empty">No archived tasks yet.</div>`);
        }
    }
}
