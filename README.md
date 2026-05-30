# FilmSearch

ASP.NET Основное приложение MVC с хранилищем PostgreSQL и отдельным сервисом рекомендаций Python/PyTorch.

## Запуск серверной части

Создайте или обновите первого пользователя-администратора с помощью переменных среды:

```powershell
$env:BootstrapAdmin__Email="admin@example.com"
$env:BootstrapAdmin__Username="admin"
$env:BootstrapAdmin__Password="admin12345"
dotnet run --project FilmSearch
```

Откройте сайт, войдите в систему как пользователь с правами администратора, затем используйте страницу `Админ` для импорта MovieLens 1M из папки, содержащей:

- `users.dat`
- `movies.dat`
- `ratings.dat`

## ML Service

Служба PyTorch находится в `ml_service`.

```powershell
cd ml_service
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

Train the model:

```powershell
python train.py --dataset-dir "C:\Users\Gouler\Desktop\Документы\практика_3_курс\ml-1m" --epochs 8
```

Run the API:

```powershell
uvicorn app:app --host 127.0.0.1 --port 8001
```

Серверная часть .NET считывает настройки ML из `MlRecommendation` в `appsettings.json`.
Если служба ML отключена, рекомендации автоматически возвращаются к базовому рекомендателю.
