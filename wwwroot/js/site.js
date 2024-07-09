// Проверка наличия signalR
if (typeof signalR === 'undefined') {
    console.error('Библиотека SignalR не загружена');  // Логирование ошибки
} else {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .build();

    connection.on("ReceiveMessage", (user, message) => {
        console.info(`[INFO] Получено сообщение от ${user}:`); // Логирование получения сообщения
        // Обновление UI с новым сообщением
        addMessageToHistory(user, message);
        scrollToBottom(); // Прокрутка в самый низ после получения нового сообщения
    });

    connection.start()
        .then(() => console.info('[INFO] Подключение SignalR успешно установлено.'))  // Логирование успешного подключения
        .catch(err => {
            console.error('[ERROR] Ошибка при установлении подключения:', err);  // Логирование ошибки подключения
        });

    // Функция для добавления сообщения в историю сообщений
    async function findRequestMessageContent(requestMessageId, user) {
        console.info(`[INFO] Запрос содержимого сообщения для requestMessageId: ${requestMessageId}, user: ${user}`);
        try {
            const content = await connection.invoke("GetMessageContent", requestMessageId, user);
            return content || 'Не найдено';
        } catch (error) {
            console.error(`[ERROR] Не удалось получить содержимое сообщения запроса для requestMessageId: ${requestMessageId}, user: ${user}:`, error);  // Логирование ошибки получения данных сообщения
            return 'Не найдено';
        }
    }

    function addMessageToHistory(user, message) {
        console.info(`[INFO] Добавление сообщения в историю сообщений. user: ${user}`);

        const messageHistoryElement = document.getElementById('messageHistory');
        if (!messageHistoryElement) {
            console.error('[ERROR] Элемент с ID "messageHistory" не найден.');  // Логирование ошибки отсутствия элемента
            return;
        }

        if (!message || !message.id) {
            console.error('[ERROR] Сообщение или ID сообщения отсутствует.');  // Логирование ошибки отсутствия сообщения или ID
            return;
        }

        const time = message.messageCurrentTime ? message.messageCurrentTime.substring(10, 16) : 'Время неизвестно';
        const requestMessageId = message.id.split("#")[0].split("$")[0];

        let messageDiv = document.createElement('div');
        if (!message.isUserMessage) {
            // Сообщение от бота
            const requestMessageId = message.id.split("#")[0]; // Извлечение ID запроса из сообщения
            const isRequestMessage = message.id.split("~")[1] === "0"; // Проверка, является ли сообщение запросом

            console.info(`[INFO] Запрашиваю содержимое сообщения для requestMessageId: ${requestMessageId}, user: ${user}`);

            findRequestMessageContent(requestMessageId, user)
                .then(requestMessageContent => {
                    messageDiv.classList.add('message-left');

                    if (isRequestMessage) {
                        // Если сообщение является запросом
                        messageDiv.innerHTML = `
                <b>Ваш запрос:</b><br>
                <div style="margin-left: 20px;">${requestMessageContent || 'Not Found'}</div>
                <br><b>Ответ:</b><br>
                <div style="margin-left: 20px;">${message.content.replace(/</g, '&lt;').replace(/>/g, '&gt;')}</div>
                <div class="time-box">${message.messageCurrentTime.substring(10, 16)}</div>`;
                    } else {
                        // Если сообщение не является запросом
                        messageDiv.innerHTML = `
                <div style="margin-left: 20px;">${message.content.replace(/</g, '&lt;').replace(/>/g, '&gt;')}</div>
                <div class="time-box">${message.messageCurrentTime.substring(10, 16)}</div>`;
                    }

                    messageHistoryElement.appendChild(messageDiv);
                })
                .catch(error => {
                    console.error(`[ERROR] Не удалось получить содержимое сообщения запроса для requestMessageId: ${requestMessageId}, user: ${user}:`, error);
                });
        }
        scrollToBottom();
    }

    function scrollToBottom() {
        const messageHistory = document.getElementById("messageHistory");
        if (messageHistory) {
            messageHistory.scrollTop = messageHistory.scrollHeight;
            console.info('[INFO] Прокрутка к нижней части истории сообщений.');
        } else {
            console.error('[ERROR] Элемент с ID "messageHistory" не найден при прокрутке.');
        }
    }

    // Функция для обновления времени
    async function updatetime() {
        try {
            // Выполняем запрос к /UpdateTime
            const response = await fetch('/UpdateTime');
            if (!response.ok) {
                throw new Error('Сетевая ошибка: Невозможно получить время с сервера');
            }

            // Получаем данные из ответа
            const data = await response.json();

            // Обновляем содержимое элемента с id="currentTime"
            document.getElementById('currentTime').textContent = `Время на сервере: ${data.time}`;
        } catch (error) {
            console.error('Ошибка при запросе времени:', error.message);
        }
    }

    // Устанавливаем интервал для вызова функции updatetime каждую секунду
    setInterval(updatetime, 1000);

    // Вызываем функцию сразу при загрузке страницы
    updatetime();


    scrollToBottom();
}
