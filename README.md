# Awesome Files

Решение тестового задания без задач со звёздочкой.

## Архитектура

Репозиторий состоит из двух приложений:

- `AwesomeFiles.Api` — ASP.NET Core Web API.
- `AwesomeFiles.Client` — консольный клиент для работы с API.

### Что делает `AwesomeFiles.Api`

API предоставляет 4 endpoint:

1. `GET /api/files` — список доступных файлов.
2. `POST /api/archives` — старт архивирования списка файлов, возвращает `taskId`.
3. `GET /api/archives/{taskId}/status` — статус задачи (`Queued`, `InProgress`, `Completed`, `Failed`).
4. `GET /api/archives/{taskId}/download` — скачивание архива, если готов.

Важные детали:

- Состояние задач хранится только в памяти (`in-memory`), после перезапуска сервиса очищается.
- Архивация выполняется асинхронно в фоне.
- Логирование HTTP-запросов включено в консоль.
- Ошибки валидации (например, несуществующие файлы) возвращаются пользователю через `400 Bad Request`.

### Что делает `AwesomeFiles.Client`

Клиент запускает интерактивный режим и поддерживает команды:

- `list`
- `create-archive <file1> <file2> ...`
- `status <taskId>`
- `download <taskId> <targetFolder>`
- `help`
- `exit`

## Папки с файлами и архивами

Настраиваются в `AwesomeFiles.Api/appsettings.json`:

```json
"Storage": {
  "FilesPath": "Data/Files",
  "ArchivesPath": "Data/Archives"
}
```

По умолчанию:

- исходные файлы: `AwesomeFiles.Api/Data/Files`
- созданные архивы: `AwesomeFiles.Api/Data/Archives`

## Как запустить

### 1) Backend API

Из корня решения:

```powershell
dotnet run --project AwesomeFiles.Api
```

API стартует на `http://localhost:5272` (по профилю `http`).

Swagger: `http://localhost:5272/swagger`

### 2) Console Client

В отдельном терминале:

```powershell
dotnet run --project AwesomeFiles.Client
```

Или с явным URL backend:

```powershell
dotnet run --project AwesomeFiles.Client -- http://localhost:5272
```

## Пример сценария

```text
> list
file1.txt file2.txt file3.txt

> create-archive file1.txt file2.txt
Create archive task is started, id: 1

> status 1
Task 1: InProgress

> status 1
Task 1: Completed

> download 1 D:\temp
Archive downloaded: D:\temp\1.zip
```

## Быстрая ручная проверка API

Файл `AwesomeFiles.Api/AwesomeFiles.Api.http` содержит готовые HTTP-запросы для всех endpoint.
