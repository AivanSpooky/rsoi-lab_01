# Лабораторная работа #1

![GitHub Classroom Workflow](../../workflows/GitHub%20Classroom%20Workflow/badge.svg?branch=master)

## Continuous Integration & Continuous Delivery

### Формулировка

В рамках первой лабораторной работы требуется написать простейшее веб приложение, предоставляющее пользователю набор
операций над сущностью Person. Для этого приложения автоматизировать процесс сборки, тестирования и релиза на Heroku.

Приложение должно реализовать API:

* `GET /persons/{personId}` – информация о человеке;
* `GET /persons` – информация по всем людям;
* `POST /persons` – создание новой записи о человеке;
* `PATCH /persons/{personId}` – обновление существующей записи о человеке;
* `DELETE /persons/{personId}` – удаление записи о человеке.

[Описание API](person-service.yaml) в формате OpenAPI.

### Требования

* Исходный проект хранится на Github. Для сборки использовать
  _только_ [Github Actions](https://docs.github.com/en/actions).
* Запросы / ответы должны быть в формате JSON.
* Если запись по id не найдена, то возвращать HTTP статус 404 Not Found.
* При создании новой записи о человека (метод POST /person) возвращать HTTP статус 201 Created с пустым телом и
  Header `Location: /api/v1/persons/{personId}`, где `personId` – id созданной записи.
* Приложение должно содержать 4-5 unit-тестов на реализованные операции.
* Приложение должно быть завернуто в Docker.
* Деплой на Heroku реализовать средствами GitHub Actions, для деплоя использовать docker. Для деплоя _нельзя_
  использовать Heroku CLI или webhooks.
* В [build.yml](.github/workflows/classroom.yml) дописать шаги на сборку, прогон unit-тестов и деплой на Heroku.
* Приложение должно использовать БД для хранения записей.
* В [[inst][heroku] Lab1.postman_environment.json](postman/%5Binst%5D%5Bheroku%5D%20Lab1.postman_environment.json)
  заменить значение `baseUrl` на адрес развернутого сервиса на Heroku.

### Пояснения

* [Пример](https://github.com/Romanow/person-service) приложения на Kotlin / Spring.
* Для локальной разработки можно использовать Postgres в docker, для этого нужно запустить `docker compose up -d`,
  поднимется контейнер с Postgres 13, будет создана БД `persons` и пользователь `program:test`.
* После успешного деплоя на Heroku, через newman запускаются интеграционные тесты. Интеграционные тесты можно проверить
  локально, для этого нужно импортировать в Postman
  коллекцию [lab1.postman_collection.json](postman/%5Binst%5D%20Lab1.postman_collection.json)]) и
  environment [[local] lab1.postman_environment.json](postman/%5Binst%5D%5Blocal%5D%20Lab1.postman_environment.json).
* Для поиска нужного инструмента для сборки используется [Github Marketplace](https://github.com/marketplace).
* Пояснение как работает [Heroku](https://devcenter.heroku.com/articles/how-heroku-works).
* Для подключения БД на Heroku заходите через Dashboard в раздел Resources и в блоке `Add-ons` ищете Heroku Postgres.
  Для получения адреса, пользователя и пароля переходите в саму БД и выбираете раздел `Settings`
  -> `Database Credentials`.
* ❗Heroku не позволяет регистрировать новых пользователей, поэтому для регистрации используйте VPN.

### Прием задания

1. При получении задания у вас создается fork этого репозитория для вашего пользователя.
2. После того как все тесты успешно завершатся, в Github Classroom на Dashboard будет отмечен успешный прогон тестов.
3. ❗️С конца
   ноября [Heroku убирает Free Plan](https://help.heroku.com/RSBRUH58/removal-of-heroku-free-product-plans-faq),
   останутся только платные подписки. В связи с этим, дедлайн по сдаче ЛР #1 10 ноября. 

---

# Реализация

> Heroku заменён на **Render** (см. [DEPLOY.md](DEPLOY.md)).

## Стек

* .NET 10 / ASP.NET Core Web API (C#)
* Entity Framework Core 10 + Npgsql, схема накатывается миграциями при старте
* PostgreSQL 13
* xUnit + FluentAssertions — 11 unit-тестов
* Docker (multi-stage build) + GitHub Actions + Render

## Структура

```
src/PersonService/
  Controllers/PersonController.cs      REST-слой, маршруты /api/v1/persons
  Services/                            IPersonService + PersonServiceImpl (бизнес-логика)
  Data/PersonDbContext.cs              EF Core, таблица persons
  Data/Migrations/                     миграции EF Core
  Domain/Person.cs                     сущность
  Dto/                                 PersonRequest/PersonResponse/Error/ValidationError
  Infrastructure/                      обработчики исключений, разбор DATABASE_URL
  Program.cs                           композиция приложения
tests/PersonService.Tests/             unit-тесты
.github/workflows/classroom.yml        CI/CD pipeline
.github/scripts/                       скрипты деплоя и ожидания сервиса
```

## API

| Метод    | Путь                    | Ответ                                                        |
|----------|-------------------------|--------------------------------------------------------------|
| `GET`    | `/api/v1/persons`       | `200` — массив `PersonResponse`                               |
| `GET`    | `/api/v1/persons/{id}`  | `200` — `PersonResponse`, `404` — `ErrorResponse`             |
| `POST`   | `/api/v1/persons`       | `201` с `Location: /api/v1/persons/{id}` и пустым телом, `400` — `ValidationErrorResponse` |
| `PATCH`  | `/api/v1/persons/{id}`  | `200` — обновлённый `PersonResponse`, `400`, `404`            |
| `DELETE` | `/api/v1/persons/{id}`  | `204`, `404`                                                  |
| `GET`    | `/manage/health`        | `200` — `{"status":"UP"}` (health check для Render)           |

`PATCH` — частичное обновление: поля, отсутствующие в теле запроса, остаются прежними.

Swagger UI доступен по адресу `/swagger`.

## Локальный запуск

Полный стек (Postgres + приложение) в Docker:

```bash
docker compose up -d --build
curl http://localhost:8080/manage/health
```

Только БД, приложение — из исходников:

```bash
docker compose up -d postgres
dotnet run --project src/PersonService
```

Строка подключения берётся из `DATABASE_URL` (формат `postgresql://user:pass@host:port/db`),
а если её нет — из `ConnectionStrings:PersonsDb` в `appsettings.json`.

## Тесты

```bash
dotnet test                                                     # unit-тесты

npx newman run "postman/[inst] Lab1.postman_collection.json" \
  -e "postman/[inst][local] Lab1.postman_environment.json"      # интеграционные
```

## CI/CD

[`.github/workflows/classroom.yml`](.github/workflows/classroom.yml) на каждый push в `master`:

1. `dotnet restore` / `build` / `test` (результаты тестов — в артефактах сборки);
2. собирает Docker-образ (`docker/build-push-action`, кеш в GitHub Actions cache);
3. поднимает `docker compose` с этим образом и гоняет newman против `localhost:8080` —
   smoke-тест до выкатки;
4. пушит образ в `ghcr.io`;
5. триггерит деплой на Render через REST API и ждёт статус `live`;
6. дожидается пробуждения сервиса и запускает newman против боевого адреса;
7. отправляет отметку автогрейдеру.

Настройка секретов и сервисов — в [DEPLOY.md](DEPLOY.md).
