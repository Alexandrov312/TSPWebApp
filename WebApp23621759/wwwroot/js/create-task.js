document.addEventListener("DOMContentLoaded", function () {
    const container = document.getElementById("subtasks-container");
    const addButton = document.getElementById("add-subtask-btn");
    const template = document.getElementById("subtask-template");
    const counter = document.getElementById("subtask-count");
    const counterWrapper = document.getElementById("subtasks-counter");
    const maxSubtasks = 15;
    const counterBlinkClass = "limit-reached-blink";
    let blinkTimeoutId;

    //Примигва брояча при опит за добавяне над лимита
    function blinkCounter() {
        clearTimeout(blinkTimeoutId);
        counterWrapper.classList.remove(counterBlinkClass);

        //Форсира reflow, за да може анимацията да се пусне отново веднага
        void counterWrapper.offsetWidth;
        counterWrapper.classList.add(counterBlinkClass);

        blinkTimeoutId = setTimeout(function () {
            counterWrapper.classList.remove(counterBlinkClass);
        }, 200);
    }

    //Връща всички текущо добавени подзадачи от формата
    function getItems() {
        return Array.from(container.querySelectorAll(".subtask-item"));
    }

    //Взима избраната зависимост от dropdown-а на конкретна подзадача
    function getSelectedDependencyIndex(item) {
        const select = item.querySelector(".subtask-dependency-select");
        if (!select || select.value === "") {
            return null;
        }

        return parseInt(select.value, 10);
    }

    //Генерира текста, който се показва в dependency dropdown-а
    function getSubtaskDisplayName(item, index) {
        const titleInput = item.querySelector(".subtask-title-input");
        const titleValue = titleInput ? titleInput.value.trim() : "";

        return titleValue.length > 0
            ? `#${index + 1} - ${titleValue}`
            : `#${index + 1}`;
    }

    //DFS
    //Проверява дали избрана зависимост би създала цикъл между подзадачите
    function createsCycle(currentIndex, candidateDependencyIndex, items) {
        const visited = new Set();

        //Обхожда веригата от зависимости, докато стигне край или се върне в началото
        function dfs(index) {
            if (index === currentIndex) {
                return true;
            }

            if (visited.has(index)) {
                return false;
            }

            visited.add(index);

            const item = items[index];
            const next = getSelectedDependencyIndex(item);

            if (next === null) {
                return false;
            }

            return dfs(next);
        }

        return dfs(candidateDependencyIndex);
    }

    //Пресмята отново валидните dependency опции за всяка подзадача
    function refreshDependencyOptions() {
        const items = getItems();

        items.forEach((item, index) => {
            const select = item.querySelector(".subtask-dependency-select");
            if (!select) {
                return;
            }

            const currentValue = select.value;
            select.innerHTML = `<option value="">No dependency</option>`;

            items.forEach((otherItem, otherIndex) => {
                if (otherIndex === index) {
                    return;
                }

                //не позволява цикли
                if (createsCycle(index, otherIndex, items)) {
                    return;
                }

                const option = document.createElement("option");
                option.value = otherIndex;
                option.textContent = getSubtaskDisplayName(otherItem, otherIndex);
                select.appendChild(option);
            });

            if ([...select.options].some(option => option.value === currentValue)) {
                select.value = currentValue;
            } else {
                select.value = "";
            }
        });
    }

    //Преномерира input полетата, за да може ASP.NET да bind-не масива правилно
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
            counterWrapper.classList.add("limit-reached");
        } else {
            addButton.classList.remove("limit-reached");
            counterWrapper.classList.remove("limit-reached");
        }

        refreshDependencyOptions();
    }

    //Добавя нова подзадача до лимита и при опит над лимита само подсказва визуално.
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

    //Управлява сгъването на описанието и премахването на подзадача от CreateTask формата.
    container.addEventListener("click", function (event) {
        const descriptionToggle = event.target.closest(".subtask-description-toggle");
        if (descriptionToggle) {
            const block = descriptionToggle.closest(".subtask-description-block");
            const isOpen = block?.classList.toggle("description-open");
            descriptionToggle.setAttribute("aria-expanded", String(!!isOpen));
            return;
        }

        const removeButton = event.target.closest(".remove-subtask-btn");
        if (!removeButton) {
            return;
        }

        const item = removeButton.closest(".subtask-item");
        if (item) {
            item.remove();
            refreshIndexes();
        }
    });

    //При промяна на заглавие обновява dependency dropdown-ите, защото текстът им зависи от него.
    container.addEventListener("input", function (event) {
        if (event.target.classList.contains("subtask-title-input")) {
            refreshDependencyOptions();
        }
    });

    //При смяна на dependency пресмята наново валидните опции и предотвратява цикли.
    container.addEventListener("change", function (event) {
        if (event.target.classList.contains("subtask-dependency-select")) {
            refreshDependencyOptions();
        }
    });

    refreshIndexes();
});
