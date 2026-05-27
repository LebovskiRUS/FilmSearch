from __future__ import annotations

import torch
from torch import nn


class MatrixFactorization(nn.Module):
    def __init__(self, users_count: int, movies_count: int, embedding_dim: int = 64) -> None:
        super().__init__()
        self.user_embeddings = nn.Embedding(users_count, embedding_dim)
        self.movie_embeddings = nn.Embedding(movies_count, embedding_dim)
        self.user_biases = nn.Embedding(users_count, 1)
        self.movie_biases = nn.Embedding(movies_count, 1)
        self.global_bias = nn.Parameter(torch.zeros(1))

        nn.init.normal_(self.user_embeddings.weight, std=0.05)
        nn.init.normal_(self.movie_embeddings.weight, std=0.05)
        nn.init.zeros_(self.user_biases.weight)
        nn.init.zeros_(self.movie_biases.weight)

    def forward(self, user_indexes: torch.Tensor, movie_indexes: torch.Tensor) -> torch.Tensor:
        user_vector = self.user_embeddings(user_indexes)
        movie_vector = self.movie_embeddings(movie_indexes)
        interaction = (user_vector * movie_vector).sum(dim=1)
        user_bias = self.user_biases(user_indexes).squeeze(1)
        movie_bias = self.movie_biases(movie_indexes).squeeze(1)
        return interaction + user_bias + movie_bias + self.global_bias
