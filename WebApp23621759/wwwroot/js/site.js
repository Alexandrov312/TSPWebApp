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

    //показване на popup
    if (event.target.closest(".show-popup")) {
        event.preventDefault();

        //Премахва всички вече отворени popup прозорци
        document.querySelectorAll(".delete-popup")
            .forEach(p => p.classList.remove("show"));

        const wrapper = event.target.closest(".delete-wrapper");
        wrapper.querySelector(".delete-popup").classList.add("show");
    }

    //cancel бутон
    if (event.target.closest(".cancel-btn")) {
        event.target.closest(".delete-popup").classList.remove("show");
    }

    //клик извън popup
    if (!event.target.closest(".delete-wrapper")) {
        document.querySelectorAll(".delete-popup")
            .forEach(p => p.classList.remove("show"));
    }



    if (event.target.closest(".edit-btn")) {
        const currentRow = event.target.closest(".task-row");

        if (!currentRow) {
            return;
        }

        //Затваря всички други редове в edit режим
        document.querySelectorAll(".task-row.editing").forEach(row => {
            if (row !== currentRow) {
                row.classList.remove("editing");
                resetRowInputs(row);
            }
        });

        currentRow.classList.add("editing");
        return;
    }

    //Натискане на cancel edit бутон
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