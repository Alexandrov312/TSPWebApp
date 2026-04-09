// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//При зареждане на страницата се изпълнява тази функция
document.addEventListener("DOMContentLoaded", function () {
    //Намира всички елементи в страницата, които имат клас .toast-notification
    const toasts = document.querySelectorAll(".toast-notification");

    toasts.forEach((toast, index) => {
        setTimeout(() => {
            toast.style.opacity = "0";
            toast.style.transform = "translateY(-10px)";
            toast.style.transition = "0.3s ease";

            setTimeout(() => toast.remove(), 300);
        }, 3000 + index * 300);
    });
});

//Слуша за всеки клик
document.addEventListener("click", function (event) {

    // DONE POPUP
    if (event.target.closest(".show-done-popup")) {
        event.preventDefault();

        document.querySelectorAll(".done-popup")
            .forEach(p => p.classList.remove("show"));

        document.querySelectorAll(".done-wrapper")
            .forEach(w => w.classList.remove("popup-open"));

        document.querySelectorAll(".delete-popup")
            .forEach(p => p.classList.remove("show"));

        document.querySelectorAll(".delete-wrapper")
            .forEach(w => w.classList.remove("popup-open"));

        const wrapper = event.target.closest(".done-wrapper");
        const popup = wrapper.querySelector(".done-popup");

        wrapper.classList.add("popup-open");
        popup.classList.add("show");
        return;
    }

    // cancel done
    if (event.target.closest(".cancel-done-btn")) {
        const popup = event.target.closest(".done-popup");
        const wrapper = event.target.closest(".done-wrapper");

        if (popup) {
            popup.classList.remove("show");
        }

        if (wrapper) {
            wrapper.classList.remove("popup-open");
        }

        return;
    }

    // DELETE POPUP
    if (event.target.closest(".show-popup")) {
        event.preventDefault();

        document.querySelectorAll(".delete-popup")
            .forEach(p => p.classList.remove("show"));

        document.querySelectorAll(".delete-wrapper")
            .forEach(w => w.classList.remove("popup-open"));

        document.querySelectorAll(".done-popup")
            .forEach(p => p.classList.remove("show"));

        document.querySelectorAll(".done-wrapper")
            .forEach(w => w.classList.remove("popup-open"));

        const wrapper = event.target.closest(".delete-wrapper");
        const popup = wrapper.querySelector(".delete-popup");

        wrapper.classList.add("popup-open");
        popup.classList.add("show");
        return;
    }

    // cancel delete
    if (event.target.closest(".cancel-btn")) {
        const popup = event.target.closest(".delete-popup");
        const wrapper = event.target.closest(".delete-wrapper");

        if (popup) {
            popup.classList.remove("show");
        }

        if (wrapper) {
            wrapper.classList.remove("popup-open");
        }

        return;
    }

    // click outside popups
    if (!event.target.closest(".delete-wrapper") && !event.target.closest(".done-wrapper")) {
        document.querySelectorAll(".delete-popup, .done-popup")
            .forEach(p => p.classList.remove("show"));

        document.querySelectorAll(".delete-wrapper, .done-wrapper")
            .forEach(w => w.classList.remove("popup-open"));
    }

    // edit
    if (event.target.closest(".edit-btn")) {
        const currentRow = event.target.closest(".task-row");

        if (!currentRow) {
            return;
        }

        document.querySelectorAll(".task-row.editing").forEach(row => {
            if (row !== currentRow) {
                row.classList.remove("editing");
                resetRowInputs(row);
            }
        });

        currentRow.classList.add("editing");
        return;
    }

    // cancel edit
    if (event.target.closest(".cancel-edit-btn")) {
        const row = event.target.closest(".task-row");

        if (!row) {
            return;
        }

        row.classList.remove("editing");
        resetRowInputs(row);
        return;
    }
});

//Връща input/select стойностите към оригиналните
function resetRowInputs(row) {
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