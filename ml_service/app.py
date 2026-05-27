from __future__ import annotations

import os
from pathlib import Path

import torch
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from model import MatrixFactorization


class RatingDto(BaseModel):
    movieLensId: int
    rating: int = Field(ge=1, le=5)


class RecommendationRequest(BaseModel):
    userId: int | None = None
    ratings: list[RatingDto] = Field(default_factory=list)
    candidates: list[int] = Field(default_factory=list)
    take: int = Field(default=10, ge=1, le=100)


class RecommendationItem(BaseModel):
    movieLensId: int
    score: float


class RecommendationResponse(BaseModel):
    modelVersion: str
    recommendations: list[RecommendationItem]


class Recommender:
    def __init__(self, model_path: Path) -> None:
        checkpoint = torch.load(model_path, map_location="cpu")
        self.movie_to_index: dict[str, int] = checkpoint["movie_to_index"]
        self.index_to_movie: list[int] = checkpoint["index_to_movie"]
        self.popular_movie_ids: list[int] = checkpoint["popular_movie_ids"]
        self.global_mean: float = float(checkpoint["global_mean"])

        self.model = MatrixFactorization(
            users_count=1,
            movies_count=len(self.index_to_movie),
            embedding_dim=int(checkpoint["embedding_dim"]),
        )
        state = checkpoint["model_state"]
        filtered_state = {
            key: value
            for key, value in state.items()
            if not key.startswith("user_embeddings") and not key.startswith("user_biases")
        }
        self.model.load_state_dict(filtered_state, strict=False)
        self.model.eval()

    def recommend(self, request: RecommendationRequest) -> RecommendationResponse:
        rated_ids = {item.movieLensId for item in request.ratings}
        candidates = request.candidates or self.index_to_movie
        candidates = [movie_id for movie_id in candidates if movie_id not in rated_ids]

        if not candidates:
            return RecommendationResponse(modelVersion="pytorch-mf", recommendations=[])

        if not request.ratings:
            recommendations = [
                RecommendationItem(movieLensId=movie_id, score=self.global_mean)
                for movie_id in self.popular_movie_ids
                if movie_id in set(candidates)
            ][: request.take]
            return RecommendationResponse(modelVersion="pytorch-mf-popular", recommendations=recommendations)

        known_ratings = [
            item for item in request.ratings if str(item.movieLensId) in self.movie_to_index
        ]
        if not known_ratings:
            recommendations = [
                RecommendationItem(movieLensId=movie_id, score=self.global_mean)
                for movie_id in self.popular_movie_ids
                if movie_id in set(candidates)
            ][: request.take]
            return RecommendationResponse(modelVersion="pytorch-mf-popular", recommendations=recommendations)

        with torch.no_grad():
            movie_embeddings = self.model.movie_embeddings.weight
            movie_biases = self.model.movie_biases.weight.squeeze(1)

            rated_indexes = torch.tensor(
                [self.movie_to_index[str(item.movieLensId)] for item in known_ratings],
                dtype=torch.long,
            )
            rating_values = torch.tensor([item.rating for item in known_ratings], dtype=torch.float32)
            weights = rating_values - self.global_mean
            if torch.all(torch.abs(weights) < 0.001):
                weights = torch.ones_like(weights)

            user_vector = (movie_embeddings[rated_indexes] * weights.unsqueeze(1)).mean(dim=0)

            candidate_pairs = [
                (movie_id, self.movie_to_index[str(movie_id)])
                for movie_id in candidates
                if str(movie_id) in self.movie_to_index
            ]

            if not candidate_pairs:
                return RecommendationResponse(modelVersion="pytorch-mf", recommendations=[])

            candidate_ids = [item[0] for item in candidate_pairs]
            candidate_indexes = torch.tensor([item[1] for item in candidate_pairs], dtype=torch.long)
            scores = (movie_embeddings[candidate_indexes] * user_vector).sum(dim=1) + movie_biases[candidate_indexes] + self.global_mean
            scores = scores.clamp(1.0, 5.0)

            top_count = min(request.take, scores.numel())
            top_scores, top_indexes = torch.topk(scores, top_count)

            recommendations = [
                RecommendationItem(movieLensId=candidate_ids[int(index)], score=float(score))
                for score, index in zip(top_scores, top_indexes)
            ]

        return RecommendationResponse(modelVersion="pytorch-mf", recommendations=recommendations)


app = FastAPI(title="FilmSearch ML Service")
model_path = Path(os.environ.get("MODEL_PATH", "artifacts/recommender.pt"))
recommender: Recommender | None = None


@app.on_event("startup")
def load_model() -> None:
    global recommender
    if model_path.exists():
        recommender = Recommender(model_path)


@app.get("/health")
def health() -> dict[str, object]:
    return {"status": "ok", "modelLoaded": recommender is not None}


@app.post("/recommendations", response_model=RecommendationResponse)
def recommendations(request: RecommendationRequest) -> RecommendationResponse:
    if recommender is None:
        raise HTTPException(status_code=503, detail=f"Model not found: {model_path}")

    return recommender.recommend(request)
