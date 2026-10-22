# PS4PKGTool for Linux (Native Port & AppImage)

<p align="center">
  <img src="PS4PKGTool/Resources/PackageIcon.png" alt="PS4PKGTool Logo" width="96" height="96" />
</p>

<p align="center">
  <b>Native Linux Port (.NET 10 + GTK3 / WebKit2GTK + OrbisPkgTool) packaged as a standalone AppImage</b><br/>
  <b>Port Nativo para Linux (.NET 10 + GTK3 / WebKit2GTK + OrbisPkgTool) empacotado como AppImage autônomo</b>
</p>

<p align="center">
  <a href="#-português-pt-br">🇧🇷 Português (PT-BR)</a> &bull;
  <a href="#-english-en-us">🇺🇸 English (EN-US)</a> &bull;
  <a href="#-credits--créditos">⭐ Credits / Créditos</a>
</p>

---

## 🇧🇷 Português (PT-BR)

### Sobre este Projeto
O **PS4PKGTool for Linux** é um **port nativo para Linux** do utilitário originalmente desenvolvido para Windows por **[pearlxcore](https://github.com/pearlxcore)** ([pearlxcore/PS4PKGTool](https://github.com/pearlxcore/PS4PKGTool) e [pearlxcore/OrbisPkgTool](https://github.com/pearlxcore/OrbisPkgTool)).

Esta versão foi inteiramente adaptada para rodar nativamente em distribuições Linux (`x86_64` — Arch Linux, Ubuntu, Debian, Fedora, SteamOS, etc.) sem necessidade de Wine ou máquinas virtuais, utilizando **.NET 10**, janela desktop nativa via **GTK3 + WebKit2GTK**, diálogos nativos do sistema e empacotamento em um único executável **`.AppImage`**.

### Principais Funcionalidades (100% Preservadas do Projeto Original)
- **Gerenciador Completo de Biblioteca PKG:**
  - Escaneamento rápido de diretórios (recursivo ou simples) e cache de manifesto (`Load Manifest`).
  - Filtros em tempo real por **Categoria** (`Base Game`, `Patch / Update`, `Add-On / DLC`, `App`), **Região**, **Compatibilidade PS5 (PS5 BC)**, **Status no shadPS4** e agrupamento dinâmico.
  - Identificação automática de **PKGs Duplicados** e **Updates/DLCs sem Jogo Base**.
  - Exportação da biblioteca completa para planilhas **Excel (`.xlsx`)**.
- **Renomeação em Lote e Organização:**
  - Todos os **12 formatos de renomeação** do projeto original + **Padrão Customizado** (`{TITLE}`, `{TITLE_ID}`, `{CONTENT_ID}`, `{VERSION}`, `{APP_VER}`, `{CATEGORY}`, `{REGION}`, `{FW}`, `{BACKPORT}`, `{SIZE}`) com pré-visualização antes de aplicar.
  - Organização e movimentação automática de arquivos `.pkg` em subpastas por Título, `Title [TitleID]`, Categoria ou Região.
  - Marcação de flag de **Backport**.
- **Inspeção Profunda de PKGs (7 Abas):**
  - **`PARAM.SFO`**: Visualização completa de todas as chaves, tipos e valores, com busca e cópia para a área de transferência.
  - **`CHANGEINFO.XML`**: Leitura das notas de atualização (*changelog*) por versão do patch.
  - **`Trophy Viewer`**: Inspeção e extração de arquivos `TROPHY.TRP` (incluindo suporte a descriptografia AES, ícones de troféus PNG, grau Bronze/Prata/Ouro/Platina e construtor de cache `NPWR`).
  - **`PKG Header & Entries`**: Inspeção de cabeçalho avançado, `PUBTOOLINFO`, listagem e extração individual ou completa de entradas brutas do PKG, além de **Validação Estrutural e Reparo de Digests Criptográficos (SHA-256)** in-place.
  - **`File Browser & Game Asset Viewer (3 Painéis)`**: Montagem direta da imagem interna `PFS` (`pfs_image.dat` / `sc0`) com suporte a *passcode*, extração seletiva de arquivos/pastas, exportação da árvore de arquivos (`.txt`) e pré-visualização integrada de assets de jogos:
    - **Texturas / Imagens**: `.gnf` (PS4 GNF), `.dds`, `.ktx`, `.png`, `.jpg`, `.bmp`, `.tga` (com exportação direta para `.png`).
    - **Áudio**: `.at9` (Sony ATRAC9 decodificado nativamente para WAV), `.wem`, `.bnk`, `.fsb`, `.ogg`, `.wav`.
    - **Containers e Engines**: Unity, Unreal Engine, CryEngine, Godot, arquivos de texto/XML/JSON e **Visualizador Hexadecimal (Hex Dump)**.
  - **`Task Queue`**: Fila persistente de tarefas em segundo plano com barra de progresso, cancelamento e repetição.
  - **`Application Log`**: Log detalhado de operações.
- **Ferramentas de Conversão, Merge, Updates e PS4 RPI:**
  - **Merge PKG**: Mesclagem nativa de Jogo Base (`Base Game PKG`) + Atualização (`Update Patch PKG`).
  - **Conversor FFPFSC**: Conversão de PKG para `FFPFSC` (*Fast-Forward PFS Compressed*) usando `libz.so.1` nativo do Linux (64-bit `LP64`), com verificação de integridade e geração de *sidecars*.
  - **Official Sony Update Downloader**: Consulta direta aos servidores da Sony por `Title ID` (`CUSAxxxxx`), exibição de tamanho/SHA-1/changelog e download das partes `.pkg`.
  - **PS4 Remote Package Installer (RPI)**: Servidor HTTP com suporte a *Byte-Range* integrado diretamente no aplicativo (sem precisar instalar Node.js) para enviar e instalar PKGs remotamente no console PS4 na rede local.
- **shadPS4 Emulator Manager (Nativo para Linux):**
  - Download e gerenciamento de builds Linux do emulador **shadPS4** (`shadps4-linux-sdl` e `shadPS4QtLauncher-linux-qt`) diretamente do GitHub Releases, com aplicação automática de permissões de execução (`chmod +x`).
  - Instalação direta de PKGs (Jogos, Updates e DLCs) na biblioteca do shadPS4.
  - Inicialização de jogos instalados com monitoramento de sessão e análise de falhas/sinais POSIX (`SIGSEGV`, `SIGABRT`, etc.).
  - Gerenciador de **Save Data** (`~/.local/share/shadPS4`) com criação e restauração de backups.
  - Download e ativação/desativação de **Patches & Cheats** oficiais do shadPS4.
  - Gerador de relatórios formatados em Markdown para o repositório oficial de compatibilidade do shadPS4.
- **Integração com o Desktop Linux e 77 Temas:**
  - **77 temas DarkUI** integrados com troca instantânea.
  - Player de música de fundo do PKG (`SND0.AT9`) e aplicação de `PIC1.PNG` / `PIC0.PNG` como **Papel de Parede do Linux** (suporte a GNOME, KDE Plasma, XFCE, MATE, Cinnamon e `feh`).
  - Integração opcional com o menu de aplicativos do Linux (`.desktop`), MIME type `application/x-ps4-pkg`, menus de contexto do **KDE Dolphin** e scripts do **GNOME Nautilus**.

### Como Executar o `.AppImage`
1. Baixe o arquivo **`PS4PKGTool-x86_64.AppImage`** na seção [Releases](../../releases) (ou gere-o localmente com `./build-appimage.sh`).
2. Dê permissão de execução e inicie:
   ```bash
   chmod +x PS4PKGTool-x86_64.AppImage
   ./PS4PKGTool-x86_64.AppImage
   ```
3. *(Opcional)* Para abrir um arquivo `.pkg` diretamente ou usar via linha de comando:
   ```bash
   ./PS4PKGTool-x86_64.AppImage /caminho/para/arquivo.pkg
   ./PS4PKGTool-x86_64.AppImage --shell validate /caminho/para/arquivo.pkg
   ```

### Como Compilar do Código-Fonte no Linux
Requisitos: **.NET 10 SDK** (`dotnet`), `gtk3`, `webkit2gtk-4.1` e `zlib`.
```bash
# Compilar e executar em modo de desenvolvimento
dotnet run --project PS4PKGTool/PS4PKGTool.csproj

# Gerar o executável autônomo PS4PKGTool-x86_64.AppImage
./build-appimage.sh
```

---

## 🇺🇸 English (EN-US)

### About This Project
**PS4PKGTool for Linux** is a **native Linux port** of the Windows utility originally created by **[pearlxcore](https://github.com/pearlxcore)** ([pearlxcore/PS4PKGTool](https://github.com/pearlxcore/PS4PKGTool) and [pearlxcore/OrbisPkgTool](https://github.com/pearlxcore/OrbisPkgTool)).

This port has been engineered to run natively on modern Linux distributions (`x86_64` — Arch Linux, Ubuntu, Debian, Fedora, SteamOS, etc.) without Wine or virtual machines, powered by **.NET 10**, a native **GTK3 + WebKit2GTK** desktop window, native GTK3 file/folder chooser dialogs, and packaged as a standalone **`.AppImage`** executable.

### Key Features (100% Feature Parity with the Original Codebase)
- **Full PKG Library Management:**
  - Fast directory scanning (recursive or flat) and cached manifest loading (`Load Manifest`).
  - Live filtering by **Category** (`Base Game`, `Patch / Update`, `Add-On / DLC`, `App`), **Region**, **PS5 Backward Compatibility (PS5 BC)**, **shadPS4 Compatibility Status**, and dynamic grouping.
  - One-click detection of **Duplicate PKGs** and **Updates/DLCs Missing a Base Game PKG**.
  - Full library export to **Excel (`.xlsx`)**.
- **Batch Rename & File Organization:**
  - All **12 original naming formats** + **Custom Pattern** (`{TITLE}`, `{TITLE_ID}`, `{CONTENT_ID}`, `{VERSION}`, `{APP_VER}`, `{CATEGORY}`, `{REGION}`, `{FW}`, `{BACKPORT}`, `{SIZE}`) with live preview before renaming.
  - Batch move/organize `.pkg` files into subfolders by Title, `Title [TitleID]`, Category, or Region.
  - Toggle **Backport** status flags.
- **Deep PKG Inspector (7 Tabs):**
  - **`PARAM.SFO`**: Full key/value/type table viewer with instant search and clipboard copy.
  - **`CHANGEINFO.XML`**: Patch changelog viewer organized by application version.
  - **`Trophy Viewer`**: Inspect and extract `TROPHY.TRP` archives (including AES decryption, trophy PNG icons, Bronze/Silver/Gold/Platinum grades, and `NPWR` trophy metadata cache builder).
  - **`PKG Header & Entries`**: Advanced PKG header fields, `PUBTOOLINFO`, raw entry table with selective or full extraction, plus **PKG Structure Validation & In-Place Cryptographic Digest Repair (SHA-256)**.
  - **`3-Pane File Browser & Game Asset Viewer`**: Directly mount internal `PFS` images (`pfs_image.dat` / `sc0`) with passcode support, selectively extract files/directories, export `.txt` file trees, and preview game assets inline:
    - **Textures / Images**: `.gnf` (PS4 GNF), `.dds`, `.ktx`, `.png`, `.jpg`, `.bmp`, `.tga` (with one-click export to `.png`).
    - **Audio**: `.at9` (Sony ATRAC9 natively decoded to WAV), `.wem`, `.bnk`, `.fsb`, `.ogg`, `.wav`.
    - **Containers & Game Engines**: Unity, Unreal Engine, CryEngine, Godot, text/XML/JSON scripts, and a built-in **Hex Dump Viewer**.
  - **`Task Queue`**: Persistent background task queue with progress tracking, pause/cancel, and retry.
  - **`Application Log`**: Real-time operation and diagnostic log.
- **PKG Merge, FFPFSC Converter, Official Update Downloader & PS4 RPI:**
  - **Merge PKG**: Native Base Game PKG + Update Patch PKG merger.
  - **FFPFSC Converter**: Convert PKGs to `FFPFSC` (*Fast-Forward PFS Compressed*) using native Linux `libz.so.1` (`LP64` 64-bit safe), with post-build verification and digest sidecars.
  - **Official Sony Update Downloader**: Query Sony's official update servers by `Title ID` (`CUSAxxxxx`), inspect piece sizes/SHA-1/changelogs, and download `.pkg` pieces with integrity verification.
  - **PS4 Remote Package Installer (RPI)**: Built-in HTTP Byte-Range file server (no external Node.js required) to stream and install PKGs directly to a PS4 console over the local network.
- **shadPS4 Emulator Manager (Linux Native):**
  - Browse, download, and manage Linux builds of **shadPS4** (`shadps4-linux-sdl` Core and `shadPS4QtLauncher-linux-qt`) directly from GitHub Releases with automatic ELF/AppImage executable permissions (`chmod +x`).
  - Queue and install PKGs (Base Games, Patches, DLCs) directly into your shadPS4 game library.
  - Launch installed games with session monitoring and POSIX signal/crash diagnostics (`SIGSEGV`, `SIGABRT`, etc.).
  - **Save Data Manager** (`~/.local/share/shadPS4`) with snapshot backups and one-click restore.
  - Download and toggle official shadPS4 **Patches & Cheats**.
  - Generate formatted Markdown compatibility reports for the official shadPS4 compatibility tracker.
- **Linux Desktop Integration & 77 Built-in Themes:**
  - **77 DarkUI themes** with live switching.
  - Automatic `SND0.AT9` background music playback and one-click Linux desktop wallpaper setting (`PIC1.PNG` / `PIC0.PNG`) across GNOME, KDE Plasma, XFCE, MATE, Cinnamon, and `feh`.
  - Optional XDG `.desktop` entry, `application/x-ps4-pkg` MIME registration, **KDE Dolphin** service menu, and **GNOME Nautilus** scripts.

### Running the `.AppImage`
1. Download **`PS4PKGTool-x86_64.AppImage`** from [Releases](../../releases) (or build it locally via `./build-appimage.sh`).
2. Mark it executable and launch:
   ```bash
   chmod +x PS4PKGTool-x86_64.AppImage
   ./PS4PKGTool-x86_64.AppImage
   ```
3. *(Optional)* Open a `.pkg` file directly in **Mini PKG Viewer** mode or run CLI shell commands:
   ```bash
   ./PS4PKGTool-x86_64.AppImage /path/to/game.pkg
   ./PS4PKGTool-x86_64.AppImage --shell validate /path/to/game.pkg
   ```

### Building from Source on Linux
Requirements: **.NET 10 SDK** (`dotnet`), `gtk3`, `webkit2gtk-4.1`, and `zlib`.
```bash
# Build and run in development mode
dotnet run --project PS4PKGTool/PS4PKGTool.csproj

# Build the standalone self-contained PS4PKGTool-x86_64.AppImage
./build-appimage.sh
```

---

## ⭐ Credits / Créditos

- **Original Project & Author / Projeto e Autor Original:**
  - **[pearlxcore](https://github.com/pearlxcore)**
  - Original Windows Repository: **[https://github.com/pearlxcore/PS4PKGTool](https://github.com/pearlxcore/PS4PKGTool)**
  - Original OrbisPkgTool Library: **[https://github.com/pearlxcore/OrbisPkgTool](https://github.com/pearlxcore/OrbisPkgTool)**
  - Original DarkUI Theme Library: **[https://github.com/pearlxcore/DarkUI](https://github.com/pearlxcore/DarkUI)**
- **Linux Native Port & AppImage Packaging / Port Nativo para Linux e Empacotamento AppImage:**
  - Maintained and published by **[gabrieelhm](https://github.com/gabrieelhm)**.

All credit for the original architecture, reverse-engineering tools, `OrbisPkgTool` container engine, and feature design belongs to **[pearlxcore](https://github.com/pearlxcore)** and the contributors of the original project.
Todo o crédito pela arquitetura original, ferramentas de engenharia reversa, motor `OrbisPkgTool` e design de funcionalidades pertence a **[pearlxcore](https://github.com/pearlxcore)** e aos colaboradores do projeto original.
