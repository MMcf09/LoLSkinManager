# Repositório de pacotes `.fantome`

Esta pasta é usada pelo LoL Skin Manager para mapear cards do catálogo para pacotes `.fantome` hospedados neste repositório.

## Estrutura

```text
skins/
├── index.json
└── packages/
    ├── Ahri/
    │   └── exemplo.fantome
    └── Lux/
        └── exemplo.fantome
```

## Formato do `index.json`

```json
[
  {
    "championId": "Ahri",
    "skinNumber": 7,
    "file": "skins/packages/Ahri/ahri_exemplo.fantome",
    "displayName": "Ahri Exemplo"
  }
]
```

`skinNumber` corresponde ao campo `num` do Data Dragon. O aplicativo baixa/importa o pacote e o marca como selecionado no perfil local. Ele não injeta código, não altera memória do jogo e não contorna o Vanguard.
