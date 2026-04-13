document.addEventListener("DOMContentLoaded", function () {
    const container = document.getElementById("subtasks-container");
    const addButton = document.getElementById("add-subtask-btn");
    const template = document.getElementById("subtask-template");
    const counter = document.getElementById("subtask-count");
    const counterWrapper = document.getElementById("subtasks-counter");
    const maxSubtasks = 10;
    const counterBlinkClass = "limit-reached-blink";
    let blinkTimeoutId;

    function blinkCounter() {
        clearTimeout(blinkTimeoutId);
        counterWrapper.classList.remove(counterBlinkClass);
        void counterWrapper.offsetWidth;
        counterWrapper.classList.add(counterBlinkClass);

        blinkTimeoutId = setTimeout(function () {
            counterWrapper.classList.remove(counterBlinkClass);
        }, 200);
    }

    function getItems() {
        return Array.from(container.querySelectorAll(".subtask-item"));
    }

    function getSelectedDependencyIndex(item) {
        const select = item.querySelector(".subtask-dependency-select");
        if (!select || select.value === "") return null;
        return parseInt(select.value, 10);
    }

    function getSubtaskDisplayName(item, index) {
        const titleInput = item.querySelector(".subtask-title-input");
        const titleValue = titleInput ? titleInput.value.trim() : "";

        return titleValue.length > 0
            ? `#${index + 1} - ${titleValue}`
            : `#${index + 1}`;
    }

    //DFS
    function createsCycle(currentIndex, candidateDependencyIndex, items) {
        let visited = new Set();

        function dfs(index) {
            if (index === currentIndex) return true;

            if (visited.has(index)) return false;
            visited.add(index);

            const item = items[index];
            const next = getSelectedDependencyIndex(item);

            if (next === null) return false;

            return dfs(next);
        }

        return dfs(candidateDependencyIndex);
    }

    function refreshDependencyOptions() {
        const items = getItems();

        items.forEach((item, index) => {
            const select = item.querySelector(".subtask-dependency-select");
            if (!select) return;

            const currentValue = select.value;
            select.innerHTML = `<option value="">No dependency</option>`;

            items.forEach((otherItem, otherIndex) => {
                if (otherIndex === index) return;

                //не позволява цикли
                if (createsCycle(index, otherIndex, items)) return;

                const option = document.createElement("option");
                option.value = otherIndex;
                option.textContent = getSubtaskDisplayName(otherItem, otherIndex);
                select.appendChild(option);
            });

            if ([...select.options].some(o => o.value === currentValue)) {
                select.value = currentValue;
            } else {
                select.value = "";
            }
        });
    }

    function refreshIndexes() {
        const items = getItems();

        items.forEach((item, index) => {
            const title = item.querySelector(".subtask-item-title");
            title.textContent = `#${index + 1}`;

            const titleInput = item.querySelector('[data-name="Title"], input[name$=".Title"]');
            const descriptionInput = item.querySelector('[data-name="Description"], textarea[name$=".Description"]');
            const dependencySelect = item.querySelector('[data-name="BlockedByIndex"], select[name$=".BlockedByIndex"]');

            if (titleInput) {
                titleInput.name = `SubTasks[${index}].Title`;
                titleInput.removeAttribute("data-name");
            }

            if (descriptionInput) {
                descriptionInput.name = `SubTasks[${index}].Description`;
                descriptionInput.removeAttribute("data-name");
            }

            if (dependencySelect) {
                dependencySelect.name = `SubTasks[${index}].BlockedByIndex`;
                dependencySelect.removeAttribute("data-name");
            }
        });

        counter.textContent = items.length;

        if (items.length >= maxSubtasks) {
            addButton.classList.add("limit-reached");
        } else {
            addButton.classList.remove("limit-reached");
        }

        refreshDependencyOptions();
    }

    addButton.addEventListener("click", function () {
        const items = getItems();

        if (items.length >= maxSubtasks) {
            addButton.classList.add("limit-reached");
            blinkCounter();
            return;
        }

        addButton.classList.remove("limit-reached");

        const clone = template.content.cloneNode(true);
        container.appendChild(clone);
        refreshIndexes();
    });

    container.addEventListener("click", function (e) {
        const removeButton = e.target.closest(".remove-subtask-btn");
        if (!removeButton) return;

        const item = removeButton.closest(".subtask-item");
        if (item) {
            item.remove();
            refreshIndexes();
        }
    });

    container.addEventListener("input", function (e) {
        if (e.target.classList.contains("subtask-title-input")) {
            refreshDependencyOptions();
        }
    });

    container.addEventListener("change", function (e) {
        if (e.target.classList.contains("subtask-dependency-select")) {
            refreshDependencyOptions();
        }
    });

    refreshIndexes();
});
