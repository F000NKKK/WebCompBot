// Проверка наличия signalR
if (typeof signalR === 'undefined') {
    console.error('Библиотека SignalR не загружена');  // Логирование ошибки
} else {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .build();

    connection.on("ReceiveMessage", (user, message) => {
        console.info(`[INFO] Получено сообщение от ${user}:`, message); // Логирование получения сообщения
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
            console.info(`[INFO] Содержимое сообщения запроса получено для requestMessageId: ${requestMessageId}, user: ${user}:`, content);
            return content || 'Не найдено';
        } catch (error) {
            console.error(`[ERROR] Не удалось получить содержимое сообщения запроса для requestMessageId: ${requestMessageId}, user: ${user}:`, error);  // Логирование ошибки получения данных сообщения
            return 'Не найдено';
        }
    }

    function addMessageToHistory(user, message) {
        console.info(`[INFO] Добавление сообщения в историю сообщений. user: ${user}, message:`, message);

        const messageHistoryElement = document.getElementById('messageHistory');
        if (!messageHistoryElement) {
            console.error('[ERROR] Элемент с ID "messageHistory" не найден.');  // Логирование ошибки отсутствия элемента
            return;
        }

        if (!message || !message.Id) {
            console.error('[ERROR] Сообщение или ID сообщения отсутствует.');  // Логирование ошибки отсутствия сообщения или ID
            console.info('[INFO] Полученные данные сообщения:', { user, message });
            return;
        }

        const time = message.MessageCurrentTime ? message.MessageCurrentTime.substring(10, 16) : 'Время неизвестно';
        const requestMessageId = message.Id.split("#")[0].split("$")[0];

        let messageDiv = document.createElement('div');
        if (message.IsUserMessage) {
            // Сообщение от пользователя
            messageDiv.classList.add('message-right');
            messageDiv.innerHTML = `
                ${message.Content}
                <div class="time-box">${time}</div>
            `;
            messageHistoryElement.appendChild(messageDiv);
            console.info(`[INFO] Добавлено сообщение от пользователя: ${message.Content}, время: ${time}`);
            scrollToBottom(); // Прокрутка в самый низ после добавления нового сообщения
        } else {
            // Сообщение от бота
            console.info(`[INFO] Запрашиваю содержимое сообщения для requestMessageId: ${requestMessageId}, user: ${user}`);
            findRequestMessageContent(requestMessageId, user).then(requestMessageContent => {
                messageDiv.classList.add('message-left');
                messageDiv.innerHTML = `
                    <b>Ваш запрос:</b><br>
                    <div style="margin-left: 20px;">${requestMessageContent}</div>
                    <br><b>Ответ:</b><br>
                    <div style="margin-left: 20px;">${message.Content}</div>
                    <div class="time-box">${time}</div>
                `;
                messageHistoryElement.appendChild(messageDiv);
                console.info(`[INFO] Добавлено сообщение от бота: ${message.Content}, время: ${time}, запрос: ${requestMessageContent}`);
                scrollToBottom(); // Прокрутка в самый низ после добавления нового сообщения
            }).catch(error => {
                console.error(`[ERROR] Не удалось получить содержимое сообщения запроса для requestMessageId: ${requestMessageId}, user: ${user}:`, error);  // Логирование ошибки получения данных сообщения
            });
        }
    }

    // Функция для инициализации начальной загрузки данных истории сообщений при загрузке страницы
    function initializeMessageHistory(initialMessages) {
        console.info('[INFO] Инициализация истории сообщений с начальными данными.');  // Логирование инициализации истории сообщений

        if (!Array.isArray(initialMessages)) {
            console.error('[ERROR] Данные сообщений не являются массивом.');  // Логирование ошибки некорректного формата данных
            console.info('[INFO] Данные initialMessages:', initialMessages);
            return;
        }

        initialMessages.forEach(item => {
            console.info(`[INFO] Обработка сообщения. user: ${item.user}, message:`, item.message);
            if (item.user && item.message) {
                addMessageToHistory(item.user, item.message);
            } else {
                console.error('[ERROR] Недопустимые данные сообщения. user:', item.user, 'message:', item.message);  // Логирование ошибки некорректных данных
            }
        });
    }

    // Пример вызова функции при загрузке страницы, если у вас есть начальные данные
    const initialMessages = window.initialMessages || [];  // Добавил пустой массив по умолчанию
    initializeMessageHistory(initialMessages);

    function scrollToBottom() {
        const messageHistory = document.getElementById("messageHistory");
        if (messageHistory) {
            messageHistory.scrollTop = messageHistory.scrollHeight;
            console.info('[INFO] Прокрутка к нижней части истории сообщений.');
        } else {
            console.error('[ERROR] Элемент с ID "messageHistory" не найден при прокрутке.');
        }
    }
}
