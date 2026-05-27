# FilmSearch ML Service

Python/FastAPI service for PyTorch-based MovieLens recommendations.

## Setup

```powershell
cd ml_service
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

## Train

```powershell
python train.py --dataset-dir "C:\Users\Gouler\Desktop\Документы\практика_3_курс\ml-1m" --epochs 8
```

The model is saved to `artifacts/recommender.pt`.

## Run API

```powershell
uvicorn app:app --host 127.0.0.1 --port 8001
```

Health check:

```powershell
curl http://127.0.0.1:8001/health
```
