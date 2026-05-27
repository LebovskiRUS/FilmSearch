# FilmSearch

ASP.NET Core MVC application with PostgreSQL storage and a separate Python/PyTorch recommendation service.

## Backend Startup

Create or update the first admin user through environment variables:

```powershell
$env:BootstrapAdmin__Email="admin@example.com"
$env:BootstrapAdmin__Username="admin"
$env:BootstrapAdmin__Password="admin12345"
dotnet run --project FilmSearch
```

Open the site, sign in as the admin user, then use the `Админ` page to import MovieLens 1M from the folder containing:

- `users.dat`
- `movies.dat`
- `ratings.dat`

## ML Service

The PyTorch service lives in `ml_service`.

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

The .NET backend reads ML settings from `MlRecommendation` in `appsettings.json`.
If the ML service is offline, recommendations automatically fall back to the baseline recommender.
