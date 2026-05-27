from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

import numpy as np
import torch
from torch import nn
from torch.utils.data import DataLoader, TensorDataset

from model import MatrixFactorization


def read_ratings(dataset_dir: Path) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    user_ids: list[int] = []
    movie_ids: list[int] = []
    ratings: list[float] = []

    with (dataset_dir / "ratings.dat").open("r", encoding="latin-1") as file:
        for line in file:
            user_id, movie_id, rating, _ = line.strip().split("::")
            user_ids.append(int(user_id))
            movie_ids.append(int(movie_id))
            ratings.append(float(rating))

    return (
        np.asarray(user_ids, dtype=np.int64),
        np.asarray(movie_ids, dtype=np.int64),
        np.asarray(ratings, dtype=np.float32),
    )


def build_index(values: np.ndarray) -> tuple[dict[int, int], list[int]]:
    unique_values = sorted(int(value) for value in np.unique(values))
    return {value: index for index, value in enumerate(unique_values)}, unique_values


def encode(values: np.ndarray, mapping: dict[int, int]) -> np.ndarray:
    return np.asarray([mapping[int(value)] for value in values], dtype=np.int64)


def rmse(predictions: torch.Tensor, targets: torch.Tensor) -> float:
    return math.sqrt(torch.mean((predictions - targets) ** 2).item())


def train(args: argparse.Namespace) -> None:
    dataset_dir = Path(args.dataset_dir)
    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)

    user_ids, movie_ids, ratings = read_ratings(dataset_dir)
    user_to_index, index_to_user = build_index(user_ids)
    movie_to_index, index_to_movie = build_index(movie_ids)

    user_indexes = encode(user_ids, user_to_index)
    movie_indexes = encode(movie_ids, movie_to_index)

    rng = np.random.default_rng(args.seed)
    indexes = np.arange(len(ratings))
    rng.shuffle(indexes)

    split = int(len(indexes) * 0.9)
    train_indexes = indexes[:split]
    valid_indexes = indexes[split:]

    train_dataset = TensorDataset(
        torch.from_numpy(user_indexes[train_indexes]),
        torch.from_numpy(movie_indexes[train_indexes]),
        torch.from_numpy(ratings[train_indexes]),
    )
    valid_dataset = TensorDataset(
        torch.from_numpy(user_indexes[valid_indexes]),
        torch.from_numpy(movie_indexes[valid_indexes]),
        torch.from_numpy(ratings[valid_indexes]),
    )

    train_loader = DataLoader(train_dataset, batch_size=args.batch_size, shuffle=True)
    valid_loader = DataLoader(valid_dataset, batch_size=args.batch_size)

    device = torch.device("cuda" if torch.cuda.is_available() and not args.cpu else "cpu")
    model = MatrixFactorization(
        users_count=len(index_to_user),
        movies_count=len(index_to_movie),
        embedding_dim=args.embedding_dim,
    ).to(device)
    with torch.no_grad():
        model.global_bias.fill_(float(np.mean(ratings)))

    optimizer = torch.optim.AdamW(model.parameters(), lr=args.learning_rate, weight_decay=args.weight_decay)
    loss_fn = nn.MSELoss()

    best_rmse = float("inf")
    best_state = None

    for epoch in range(1, args.epochs + 1):
        model.train()
        total_loss = 0.0

        for batch_users, batch_movies, batch_ratings in train_loader:
            batch_users = batch_users.to(device)
            batch_movies = batch_movies.to(device)
            batch_ratings = batch_ratings.to(device)

            optimizer.zero_grad()
            predictions = model(batch_users, batch_movies)
            loss = loss_fn(predictions, batch_ratings)
            loss.backward()
            optimizer.step()

            total_loss += loss.item() * batch_users.size(0)

        model.eval()
        valid_predictions: list[torch.Tensor] = []
        valid_targets: list[torch.Tensor] = []
        with torch.no_grad():
            for batch_users, batch_movies, batch_ratings in valid_loader:
                batch_users = batch_users.to(device)
                batch_movies = batch_movies.to(device)
                predictions = model(batch_users, batch_movies.to(device)).clamp(1.0, 5.0).cpu()
                valid_predictions.append(predictions)
                valid_targets.append(batch_ratings)

        epoch_rmse = rmse(torch.cat(valid_predictions), torch.cat(valid_targets))
        epoch_mae = torch.mean(torch.abs(torch.cat(valid_predictions) - torch.cat(valid_targets))).item()
        train_loss = total_loss / len(train_dataset)
        print(f"epoch={epoch} train_mse={train_loss:.4f} valid_rmse={epoch_rmse:.4f} valid_mae={epoch_mae:.4f}")

        if epoch_rmse < best_rmse:
            best_rmse = epoch_rmse
            best_state = {key: value.cpu() for key, value in model.state_dict().items()}

    if best_state is not None:
        model.load_state_dict(best_state)

    movie_counts: dict[int, int] = {}
    movie_sums: dict[int, float] = {}
    for movie_id, rating in zip(movie_ids, ratings):
        movie_id = int(movie_id)
        movie_counts[movie_id] = movie_counts.get(movie_id, 0) + 1
        movie_sums[movie_id] = movie_sums.get(movie_id, 0.0) + float(rating)

    popular_movie_ids = sorted(
        movie_counts,
        key=lambda item: (movie_counts[item], movie_sums[item] / movie_counts[item]),
        reverse=True,
    )

    checkpoint = {
        "model_state": model.state_dict(),
        "embedding_dim": args.embedding_dim,
        "index_to_movie": index_to_movie,
        "movie_to_index": {str(key): value for key, value in movie_to_index.items()},
        "global_mean": float(np.mean(ratings)),
        "popular_movie_ids": popular_movie_ids,
        "metrics": {
            "rmse": best_rmse,
        },
    }
    torch.save(checkpoint, output_path)

    metadata_path = output_path.with_suffix(".json")
    metadata_path.write_text(
        json.dumps({"movies": len(index_to_movie), "users": len(index_to_user), "rmse": best_rmse}, indent=2),
        encoding="utf-8",
    )


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--dataset-dir", required=True)
    parser.add_argument("--output", default="artifacts/recommender.pt")
    parser.add_argument("--epochs", type=int, default=8)
    parser.add_argument("--embedding-dim", type=int, default=64)
    parser.add_argument("--batch-size", type=int, default=8192)
    parser.add_argument("--learning-rate", type=float, default=0.003)
    parser.add_argument("--weight-decay", type=float, default=0.00001)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--cpu", action="store_true")
    return parser.parse_args()


if __name__ == "__main__":
    train(parse_args())
