# LoL Skin Manager

Aplicativo Windows em **C# / .NET 8 / WPF** para organizar uma biblioteca local de pacotes de custom skins.

## O que a versão inicial faz

- Importa pacotes `.zip` e `.fantome` para uma biblioteca local.
- Mantém os arquivos em `%LOCALAPPDATA%\\LoLSkinManager\\Packages`.
- Permite marcar pacotes como ativados/desativados em um perfil local.
- Remove pacotes e abre rapidamente a pasta da biblioteca.
- Salva o estado da biblioteca em JSON.

## Limite de segurança

Este projeto **não injeta DLLs, não lê/escreve memória do League of Legends e não tenta contornar o Riot Vanguard**. A versão inicial é um gerenciador de pacotes/perfis; qualquer integração futura com o jogo deve permanecer dentro de métodos permitidos pela Riot.

## Requisitos

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 (opcional, recomendado)

## Executar

```powershell
dotnet restore
dotnet run --project .\\src\\LoLSkinManager.App\\LoLSkinManager.App.csproj
```

## Compilar Release

```powershell
dotnet publish .\\src\\LoLSkinManager.App\\LoLSkinManager.App.csproj -c Release -r win-x64 --self-contained false
```

Os arquivos gerados ficam em `src\\LoLSkinManager.App\\bin\\Release\\net8.0-windows\\win-x64\\publish`.

## Próximos passos

- Preview/capa dos pacotes.
- Tags e busca.
- Perfis múltiplos.
- Validação de estrutura de pacotes.
- Instalador para Windows.
- GitHub Actions para build automático.

## Aviso

League of Legends e Riot Games são marcas de seus respectivos proprietários. Este projeto não é afiliado ou endossado pela Riot Games.
