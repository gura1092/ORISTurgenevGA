(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        const panel = document.querySelector(".acc-sign-in");
        if (!panel) return;

        const inputs = panel.querySelectorAll(".login-pass input.search-login-pass");
        const button = panel.querySelector(".button-sign-in .but-sign-in");

        if (inputs.length < 2 || !button) {
            console.error("Steam demo form: не найдены исходные поля или кнопка.");
            return;
        }

        const demoUserInput = inputs[0];
        const messageInput = inputs[1];
        const labels = panel.querySelectorAll(".login-pass .naming");

        demoUserInput.type = "text";
        demoUserInput.maxLength = 100;
        demoUserInput.autocomplete = "off";
        demoUserInput.required = true;

        messageInput.name = "message";
        messageInput.type = "text";
        messageInput.maxLength = 2000;
        messageInput.autocomplete = "off";
        messageInput.required = true;
        button.type = "button";

        panel.setAttribute("role", "form");

        let status = panel.querySelector(".steam-demo-result");
        if (!status) {
            status = document.createElement("p");
            status.className = "steam-demo-result";
            status.setAttribute("role", "status");
            status.setAttribute("aria-live", "polite");
            status.style.margin = "1px 0 0";
            status.style.color = "#c6d4df";
            status.style.fontSize = "13px";
            status.style.lineHeight = "1.5";
            const buttonContainer = panel.querySelector(".button-sign-in");
            (buttonContainer || panel).insertAdjacentElement("afterend", status);
        }

        const send = async () => {
            const demoUser = demoUserInput.value.trim();
            const message = messageInput.value.trim();

            if (!demoUser || !message) {
                status.textContent = "Заполните учебное имя и сообщение.";
                return;
            }

            button.disabled = true;
            status.textContent = "Отправка...";

            try {
                const body = new URLSearchParams({ demoUser, message });
                const response = await fetch("/auth/sendemail", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8"
                    },
                    body: body.toString(),
                    credentials: "same-origin"
                });

                const responseText = await response.text();
                status.textContent = responseText || (response.ok
                    ? "Сообщение принято сервером."
                    : `Ошибка HTTP ${response.status}.`);

                if (!response.ok) {
                    status.style.color = "#ff8b8b";
                } else {
                    status.style.color = "#a8e6a3";
                }
            } catch (error) {
                console.error("Steam demo form:", error);
                status.textContent = "Не удалось связаться с сервером. Проверьте, запущен ли MiniHttpServer.";
                status.style.color = "#ff8b8b";
            } finally {
                button.disabled = false;
            }
        };

        button.addEventListener("click", send);
        [demoUserInput, messageInput].forEach(input => {
            input.addEventListener("keydown", event => {
                if (event.key === "Enter") {
                    event.preventDefault();
                    send();
                }
            });
        });
    });
})();