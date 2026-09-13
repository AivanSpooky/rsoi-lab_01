# Деплой

Вместо Heroku используется **Render**. Схема деплоя полностью удовлетворяет требованиям ЛР:

1. GitHub Actions собирает Docker-образ и публикует его в GitHub Container Registry (`ghcr.io`).
2. GitHub Actions вызывает **публичный REST API Render** (`POST /v1/services/{serviceId}/deploys`)
   и передаёт ссылку на свежий образ.
3. Workflow ждёт, пока деплой перейдёт в статус `live`, и только потом гоняет newman.

Ни Render CLI, ни deploy hooks (webhooks) не используются — только Docker и HTTP API.

## Порядок первичной настройки

Образ должен существовать до создания сервиса, поэтому шаги идут именно так:

1. Запушить `master` → workflow опубликует `ghcr.io/<user>/<repo>:latest` (деплой пропустится с warning'ом).
2. Сделать пакет в GHCR публичным (шаг 2 ниже).
3. Создать сервис и БД на Render (шаг 1 ниже) — первый деплой сразу подтянет образ.
4. Прописать секреты и переменные (шаг 3) — дальше каждый push деплоится автоматически.

---

## 1. Создать сервис и БД на Render

### Вариант A: через Blueprint (быстрее)

В репозитории лежит [`render.yaml`](render.yaml). На [dashboard.render.com](https://dashboard.render.com)
выберите **New → Blueprint**, подключите репозиторий — Render создаст сразу и Postgres, и web-сервис.

Перед этим поправьте в `render.yaml` поле `image.url` под своё имя репозитория (всё в нижнем регистре):

```yaml
image:
  url: ghcr.io/<github-username>/<repo-name>:latest
```

### Вариант B: вручную

1. **New → Postgres**: имя `persons-db`, database `persons`, user `program`, план **Free**.
2. **New → Web Service → Existing image**:
   - Image URL: `ghcr.io/<github-username>/<repo-name>:latest`
   - Instance type: **Free**
   - Health Check Path: `/manage/health`
3. В сервисе на вкладке **Environment** добавьте переменную `DATABASE_URL` со значением
   **Internal Database URL** из созданного Postgres.

> Порт указывать не нужно: Render передаёт `PORT`, приложение читает его в `Program.cs`.

## 2. Сделать образ в GHCR публичным

Render по умолчанию тянет образ анонимно. После первого успешного push'а workflow:

GitHub → ваш профиль → **Packages** → пакет `<repo-name>` → **Package settings** →
**Change visibility** → **Public**.

Альтернатива: в Render **Settings → Registry Credentials** добавить GitHub PAT с правом `read:packages`.

## 3. Прописать секреты и переменные в GitHub

Repository → **Settings → Secrets and variables → Actions**.

### Secrets

| Имя                 | Где взять                                                                 |
|---------------------|---------------------------------------------------------------------------|
| `RENDER_API_KEY`    | Render → Account Settings → **API Keys** → Create API Key                 |
| `RENDER_SERVICE_ID` | ID сервиса вида `srv-xxxxxxxxxxxx` — виден в URL дашборда сервиса          |
| `GOOGLE_API_KEY`    | выдаётся преподавателем (для шага автогрейдера)                            |

### Variables

| Имя           | Значение                                            |
|---------------|-----------------------------------------------------|
| `SERVICE_URL` | публичный адрес сервиса, например `https://person-service.onrender.com` |

`SERVICE_URL` используется, чтобы дождаться пробуждения сервиса и подставить `baseUrl`
в Postman-окружение перед прогоном newman.

## 4. Обновить Postman-окружение

В [`postman/[inst][heroku] Lab1.postman_environment.json`](postman/%5Binst%5D%5Bheroku%5D%20Lab1.postman_environment.json)
поле `baseUrl` должно содержать адрес развёрнутого сервиса. Этого требует
[`.github/classroom/autograding.json`](.github/classroom/autograding.json), который запускает newman сам.

---

## Особенности Free-плана Render

* Сервис засыпает после 15 минут простоя, первый запрос «будит» его ~40–60 секунд.
  Поэтому перед newman workflow выполняет [`wait-for-service.sh`](.github/scripts/wait-for-service.sh),
  который опрашивает `/manage/health`.
* Бесплатный Postgres на Render живёт 30 дней. После истечения создайте новую БД
  и обновите `DATABASE_URL`, либо подключите внешний бесплатный Postgres (Neon, Supabase) —
  приложение принимает любой `postgresql://...` URL.

---

## Альтернатива: Railway

Если хочется Railway вместо Render:

1. **New Project → Empty Project → Add Service → Docker Image**, укажите
   `ghcr.io/<username>/<repo>:latest`.
2. **Add Service → Database → PostgreSQL**. Railway сам создаст переменную `DATABASE_URL`;
   в настройках сервиса добавьте referenced variable `DATABASE_URL = ${{Postgres.DATABASE_URL}}`.
3. Railway автоматически передаёт `PORT` — приложение его читает.
4. Для передеплоя из GitHub Actions вместо шага *Deploy to Render* используйте вызов
   Railway GraphQL API (`https://backboard.railway.com/graphql/v2`, mutation `serviceInstanceRedeploy`)
   с токеном из `Account Settings → Tokens`, положив его в секрет `RAILWAY_TOKEN`.
